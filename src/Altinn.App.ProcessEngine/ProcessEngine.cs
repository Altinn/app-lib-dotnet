using System.Collections.Concurrent;
using System.Diagnostics;
using Altinn.App.ProcessEngine.Exceptions;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.ProcessEngine;

internal interface IProcessEngine
{
    ProcessEngineSettings Settings { get; }
    ProcessEngineHealthStatus Status { get; }
    int InboxCount { get; }
    Task Start(CancellationToken cancellationToken = default);
    Task Stop();
    Task<ProcessEngineResponse> EnqueueJob(ProcessEngineRequest request, CancellationToken cancellationToken = default);
    bool HasQueuedJob(string jobIdentifier);
}

internal partial class ProcessEngine : IProcessEngine, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly IProcessEngineTaskHandler _taskHandler;
    private readonly Buffer<bool> _isEnabledHistory = new();
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private readonly ProcessEngineRetryStrategy _statusCheckBackoffStrategy = new(
        ProcessEngineBackoffType.Exponential,
        Delay: TimeSpan.FromSeconds(1),
        MaxDelay: TimeSpan.FromMinutes(1)
    );

    private ConcurrentDictionary<string, ProcessEngineJob> _inbox;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _mainLoopTask;
    private SemaphoreSlim _inboxCapacityLimit;
    private volatile bool _cleanupRequired;
    private bool _disposed;

    public ProcessEngineHealthStatus Status { get; private set; }
    public int InboxCount => _inbox.Count;
    public ProcessEngineSettings Settings { get; }

    public ProcessEngine(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ProcessEngine>>();
        _taskHandler = serviceProvider.GetRequiredService<IProcessEngineTaskHandler>();
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        Settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;

        InitializeInbox();
    }

    private async Task<bool> ShouldRun(CancellationToken cancellationToken)
    {
        // E.g. "do we hold the lock?"
        _logger.LogDebug("Checking if process engine should run");

        // TODO: Replace this with actual check
        bool placeholderEnabledResponse = await Task.Run(
            async () =>
            {
                await Task.Delay(100, cancellationToken);
                return true;
            },
            cancellationToken
        );

        await _isEnabledHistory.Add(placeholderEnabledResponse);

        var latest = await _isEnabledHistory.Latest();
        var previous = await _isEnabledHistory.Previous();

        // Populate queue if we just transitioned from disabled to enabled
        if (latest is true && previous is false)
            await PopulateJobsFromStorage(cancellationToken);

        // Progressive backoff we have been disabled for two or more consecutive checks
        if (latest is false && previous is false)
        {
            int iteration = await _isEnabledHistory.ConsecutiveCount(x => !x);
            var backoffDelay = _statusCheckBackoffStrategy.CalculateDelay(iteration);

            _logger.LogInformation("Process engine is disabled. Backing off for {BackoffDelay}", backoffDelay);
            await Task.Delay(backoffDelay, cancellationToken);
        }

        // Update status
        if (placeholderEnabledResponse)
        {
            Status &= ~ProcessEngineHealthStatus.Disabled;
            _logger.LogDebug("Process engine is enabled");
        }
        else
        {
            Status |= ProcessEngineHealthStatus.Disabled;
            _logger.LogDebug("Process engine is disabled");
        }
        return placeholderEnabledResponse;
    }

    private async Task<bool> HaveJobs(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking if we have jobs to process");
        bool haveJobs = InboxCount > 0;

        if (haveJobs)
        {
            _logger.LogDebug("We have jobs to process: {InboxCount}", InboxCount);
            Status &= ~ProcessEngineHealthStatus.Idle;
        }
        else
        {
            _logger.LogDebug("No jobs to process, taking a short nap");
            Status |= ProcessEngineHealthStatus.Idle;
            await Task.Delay(500, cancellationToken);
        }

        return haveJobs;
    }

    private async Task MainLoop(CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Entering MainLoop. Inbox count: {InboxCount}. Queue slots taken: {OccupiedQueueSlots}",
            InboxCount,
            _inboxCapacityLimit.CurrentCount
        );

        // Should we run?
        if (!await ShouldRun(cancellationToken))
            return;

        // Do we have jobs to process?
        if (!await HaveJobs(cancellationToken))
            return;

        // Process jobs in parallel
        _logger.LogDebug("Processing jobs. Queue size: {InboxCount}", InboxCount);
        await Parallel.ForEachAsync(
            _inbox.Values.ToList(), // Copy so we can modify the original collection during iteration
            cancellationToken,
            async (job, ct) =>
            {
                await ProcessJob(job, ct);
            }
        );

        // Small delay before next iteration
        _logger.LogDebug("Tiny nap");
        await Task.Delay(100, cancellationToken);
    }

    private async Task ProcessJob(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing job: {Job}", job);

        switch (job.DatabaseUpdateStatus())
        {
            // Process the tasks
            case ProcessEngineTaskStatus.None:
                await ProcessTasks(job, cancellationToken);
                job.Status = job.OverallStatus();
                job.DatabaseTask = UpdateJobInStorage(job, cancellationToken); // TODO: Might not need to update the job so often. Tasks are already saved. Only update here is the job status
                return;

            // Waiting on database operation to finish
            case ProcessEngineTaskStatus.Started:
                _logger.LogDebug("Job is waiting for database operation to complete");
                return;

            // Database operation is finished
            case ProcessEngineTaskStatus.Finished:
                job.CleanupDatabaseTask();
                break;

            default:
                throw new ProcessEngineException($"Unknown database update status: {job.DatabaseUpdateStatus()}");
        }

        // Job still has work pending (requeued tasks, etc)
        if (!job.IsDone())
        {
            _logger.LogDebug("Job {Job} is still has tasks processing. Leaving in queue for next iteration", job);
            return;
        }

        // Job is done (success or permanent failure). Remove and release queue slot
        RemoveJobAndReleaseQueueSlot(job);
        _logger.LogDebug("Job {Job} is done", job);
    }

    private async Task ProcessTasks(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        foreach (ProcessEngineTask task in job.OrderedIncompleteTasks())
        {
            _logger.LogDebug("Processing task: {Task}", task);

            // Not time to process yet
            if (!task.IsReadyForExecution(_timeProvider))
            {
                _logger.LogDebug("Task not ready for execution");
                return;
            }

            var currentState = new
            {
                DatabaseUpdateStatus = task.DatabaseUpdateStatus(),
                ExecutionStatus = task.ExecutionStatus(),
            };

            switch (currentState)
            {
                // Waiting for database operation to complete
                case { DatabaseUpdateStatus: ProcessEngineTaskStatus.Started }:
                    _logger.LogDebug("Task is waiting for database operation to complete");
                    return;

                // Database operation completed
                case { DatabaseUpdateStatus: ProcessEngineTaskStatus.Finished }:

                    // Cleanup and move to next task
                    task.CleanupDatabaseTask();
                    continue;

                // Waiting for execution task to complete
                case { ExecutionStatus: ProcessEngineTaskStatus.Started }:
                    _logger.LogDebug("Task is already processing, but not yet finished");
                    return;

                // Execution task completed
                case { ExecutionStatus: ProcessEngineTaskStatus.Finished }:
                    Debug.Assert(task.ExecutionTask is not null);

                    // Unwrap result and handle outcome
                    ProcessEngineExecutionResult result = await task.ExecutionTask;
                    UpdateTaskStatusAndRetryDecision(task, result);

                    // Cleanup and update database
                    task.CleanupExecutionTask();
                    task.DatabaseTask = UpdateTaskInStorage(task, cancellationToken);
                    return;

                // Task is new
                default:

                    // Start the execution task
                    task.ExecutionTask = _taskHandler.Execute(task, cancellationToken);
                    return;
            }
        }

        return;

        void UpdateTaskStatusAndRetryDecision(ProcessEngineTask task, ProcessEngineExecutionResult result)
        {
            if (result.IsSuccess())
            {
                task.Status = ProcessEngineItemStatus.Completed;
                _logger.LogDebug("Task {Task} completed successfully", task);
                return;
            }

            var retryStrategy = task.RetryStrategy ?? Settings.DefaultTaskRetryStrategy;

            if (retryStrategy.CanRetry(task.RequeueCount + 1))
            {
                task.RequeueCount++;
                task.Status = ProcessEngineItemStatus.Requeued;
                task.BackoffUntil = _timeProvider.GetUtcNow().Add(retryStrategy.CalculateDelay(task.RequeueCount));
                _logger.LogDebug("Requeuing task {Task} (Retry count: {Retries})", task, task.RequeueCount);
            }
            else
            {
                task.Status = ProcessEngineItemStatus.Failed;
                task.BackoffUntil = null;
                _logger.LogError("Failing task {Task} (Retry count: {Retries})", task, task.RequeueCount);
            }
        }
    }
}
