using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
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
}

// TODO: This is already a big boi. Partial classes? More services?

internal sealed class ProcessEngine : IProcessEngine, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly IProcessEngineTaskHandler _taskHandler;
    private readonly Buffer<bool> _enabledStatusHistory = new();
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private readonly ProcessEngineRetryStrategy _statusCheckBackoffStrategy = new(
        ProcessEngineBackoffType.Exponential,
        Delay: TimeSpan.FromSeconds(1)
    );

    private ConcurrentQueue<ProcessEngineJob> _inbox;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _mainLoopTask;
    private SemaphoreSlim _inboxCapacityLimit;
    private volatile bool _cleanupRequired;
    private bool _disposed;

    // TODO: Is this overengineered? Maybe we only care if it's running or not?
    public ProcessEngineHealthStatus Status { get; private set; } = ProcessEngineHealthStatus.Healthy;
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

    public async Task Start(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting process engine");

        if (_cancellationTokenSource is not null || _mainLoopTask is not null)
            await Stop();

        Status |= ProcessEngineHealthStatus.Running;
        _cleanupRequired = true;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _mainLoopTask = Task.Run(
            async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            await MainLoop(_cancellationTokenSource.Token);
                            Status &= ~ProcessEngineHealthStatus.Unhealthy;
                            Status |= ProcessEngineHealthStatus.Healthy;
                            Status |= ProcessEngineHealthStatus.Running;
                        }
                        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested) { }
                        catch (Exception e)
                        {
                            _logger.LogError(
                                e,
                                "The process engine encountered an unhandled exception: {Message}",
                                e.Message
                            );

                            Status |= ProcessEngineHealthStatus.Unhealthy;
                        }
                    }
                }
                finally
                {
                    await Cleanup();
                    Status &= ~ProcessEngineHealthStatus.Running;
                }
            },
            _cancellationTokenSource.Token
        );
    }

    public async Task Stop()
    {
        _logger.LogDebug("(public) Stopping process engine");

        if (!Status.HasFlag(ProcessEngineHealthStatus.Running))
            return;

        try
        {
            if (_cancellationTokenSource is not null)
                await _cancellationTokenSource.CancelAsync();

            if (_mainLoopTask?.IsCompleted is false)
                await _mainLoopTask;
        }
        catch (OperationCanceledException) { }
        finally
        {
            Status &= ~ProcessEngineHealthStatus.Running;
            Status |= ProcessEngineHealthStatus.Stopped;
        }
    }

    public async Task<ProcessEngineResponse> EnqueueJob(
        ProcessEngineRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("(public) Enqueuing job {JobIdentifier}", request.JobIdentifier);

        if (!request.IsValid())
            return ProcessEngineResponse.Rejected("Invalid request");

        if (_mainLoopTask is null)
            return ProcessEngineResponse.Rejected("Process engine is not running. Did you call Start()?");

        var enabled = await _enabledStatusHistory.Latest() ?? await ShouldRun(cancellationToken);
        if (!enabled)
            return ProcessEngineResponse.Rejected(
                "Process engine is currently inactive. Did you call the right instance?"
            );

        // TODO: Duplication check? If we already have an active job with some form of calculated ID, disallow enqueue

        await AcquireQueueSlot(cancellationToken); // Only acquire slots for public requests
        await EnqueueJob(ProcessEngineJob.FromRequest(request), true, cancellationToken);

        return ProcessEngineResponse.Accepted();
    }

    private async Task AcquireQueueSlot(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Acquiring queue slot");
        await _inboxCapacityLimit.WaitAsync(cancellationToken);

        if (InboxCount >= Settings.QueueCapacity)
            Status |= ProcessEngineHealthStatus.QueueFull;
        else
            Status &= ~ProcessEngineHealthStatus.QueueFull;

        _logger.LogDebug("Status after acquiring slot: {Status}", Status);
    }

    private void ReleaseQueueSlot()
    {
        _logger.LogDebug("Releasing queue slot");
        _inboxCapacityLimit.Release();
    }

    private async Task EnqueueJob(
        ProcessEngineJob job,
        bool updateDatabase = true,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("(internal) Enqueuing job {Job}. Update database: {UpdateDb}", job, updateDatabase);

        // TODO: persist to database if `updateDatabase` is true
        await Task.CompletedTask;

        _inbox.Enqueue(job);
    }

    private Task PopulateJobsFromStorage(CancellationToken cancellationToken)
    {
        // TODO: Populate the queue from the database. This must be a resilient call to db
        _logger.LogDebug("Populating jobs from storage");
        return Task.CompletedTask;
    }

    private Task UpdateJobInStorage(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        // TODO: Should we update the `Instance` with something here too? Like if the job has failed, etc
        // TODO: This must be a resilient call to db
        _logger.LogDebug("Updating job in storage: {Job}", job);
        return Task.CompletedTask;
    }

    private Task UpdateTaskInStorage(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        // TODO: This must be a resilient call to db
        _logger.LogDebug("Updating task in storage: {Task}", task);
        return Task.CompletedTask;
    }

    private async Task<bool> ShouldRun(CancellationToken cancellationToken)
    {
        // E.g. "do we hold the lock?"
        _logger.LogDebug("Checking if process engine should run");

        bool placeholderEnabledResponse = await Task.Run(
            async () =>
            {
                await Task.Delay(100, cancellationToken);
                return true;
            },
            cancellationToken
        );

        await _enabledStatusHistory.Add(placeholderEnabledResponse);

        var latest = await _enabledStatusHistory.Latest();
        var previous = await _enabledStatusHistory.Previous();

        // Populate queue if we just transitioned from disabled to enabled
        if (latest is true && previous is false)
            await PopulateJobsFromStorage(cancellationToken);

        // Progressive backoff we have been disabled for two or more consecutive checks
        if (latest is false && previous is false)
        {
            int iteration = await _enabledStatusHistory.ConsecutiveCount(x => !x);
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

    [MemberNotNull(nameof(_inbox), nameof(_inboxCapacityLimit))]
    private void InitializeInbox()
    {
        _inbox = new ConcurrentQueue<ProcessEngineJob>();
        _inboxCapacityLimit = new SemaphoreSlim(Settings.QueueCapacity, Settings.QueueCapacity);
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

    private IEnumerable<ProcessEngineJob> DequeueAllJobs()
    {
        while (_inbox.TryDequeue(out var job))
        {
            yield return job;
        }
    }

    private async Task MainLoop(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Entering MainLoop. Inbox count: {InboxCount}", InboxCount);

        // Should we run?
        if (!await ShouldRun(cancellationToken))
            return;

        // Do we have jobs to process?
        if (!await HaveJobs(cancellationToken))
            return;

        // Process jobs in parallel
        _logger.LogDebug("Processing jobs. Queue size: {InboxCount}", InboxCount);
        ConcurrentBag<ProcessEngineJob> requeue = [];
        await Parallel.ForEachAsync(
            DequeueAllJobs(),
            cancellationToken,
            async (item, ct) =>
            {
                await ProcessJob(item, requeue, ct);
            }
        );

        // Requeue jobs
        foreach (var job in requeue)
        {
            await EnqueueJob(job, updateDatabase: false, cancellationToken);
        }

        // Small delay before next iteration
        _logger.LogDebug("Tiny nap");
        await Task.Delay(100, cancellationToken);
    }

    private async Task ProcessJob(
        ProcessEngineJob job,
        ConcurrentBag<ProcessEngineJob> requeue,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Processing job: {Job}", job);

        foreach (var task in job.OrderedIncompleteTasks())
        {
            _logger.LogDebug("Processing task: {Task}", task);

            // Not time to process yet
            if (!task.IsReadyForExecution(_timeProvider))
            {
                _logger.LogDebug("Task not ready for execution");
                break;
            }

            // Already processing and not finished
            if (task.ExecutionTask?.IsCompleted is false)
            {
                _logger.LogDebug("Task is already processing, but not yet finished");
                break;
            }

            // Execute job instructions
            task.ExecutionTask ??= _taskHandler.Execute(task, cancellationToken);

            // Background wait
            if (
                !task.ExecutionTask.IsCompleted
                && task.Command.ExecutionStrategy == ProcessEngineTaskExecutionStrategy.PeriodicPolling
            )
            {
                _logger.LogDebug("Performing background wait for task {Task}", task);
                task.Status = ProcessEngineItemStatus.Processing; // Don't persist this, we're dependent on the in-memory task anyway
                break;
            }

            // Foreground wait
            var result = await task.ExecutionTask;

            // Success
            if (result.IsSuccess())
            {
                await TaskSucceeded(task);
                continue;
            }

            // Error
            await TaskFailed(task);
            break;
        }

        // Job needs requeue or is done
        job.Status = job.OverallStatus();
        await UpdateJobInStorage(job, cancellationToken);

        if (job.Status.IsDone())
        {
            _logger.LogDebug("Job {Job} is done", job);
            ReleaseQueueSlot();
        }
        else
        {
            _logger.LogDebug("Job {Job} is still processing, slating for requeue", job);
            requeue.Add(job);
        }

        return;

        async Task TaskSucceeded(ProcessEngineTask task)
        {
            _logger.LogDebug("Task {Task} completed successfully", task);
            task.ExecutionTask?.Dispose();
            task.ExecutionTask = null;
            task.Status = ProcessEngineItemStatus.Completed;
            await UpdateTaskInStorage(task, cancellationToken);
        }

        async Task TaskFailed(ProcessEngineTask task)
        {
            task.RequeueCount++;
            task.ExecutionTask?.Dispose();
            task.ExecutionTask = null;

            var retryStrategy = task.RetryStrategy ?? Settings.DefaultTaskRetryStrategy;
            if (retryStrategy.CanRetry(task.RequeueCount))
            {
                _logger.LogDebug("Requeuing task {Task} (Retry count: {Retries})", task, task.RequeueCount);
                task.Status = ProcessEngineItemStatus.Requeued;
                task.BackoffUntil = _timeProvider.GetUtcNow().Add(retryStrategy.CalculateDelay(task.RequeueCount));
            }
            else
            {
                _logger.LogError("Failing task {Task} (Retry count: {Retries})", task, task.RequeueCount);
                task.Status = ProcessEngineItemStatus.Failed;
            }

            await UpdateTaskInStorage(task, cancellationToken);
        }
    }

    private async Task Cleanup()
    {
        await _cleanupLock.WaitAsync();

        try
        {
            if (!_cleanupRequired)
                return;

            _cleanupRequired = false;
            _mainLoopTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            await _enabledStatusHistory.Clear();

            _inbox.Clear();
            _inboxCapacityLimit.Dispose();

            InitializeInbox();
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource?.Dispose();
        _mainLoopTask?.Dispose();
        _enabledStatusHistory.Dispose();
        _cleanupLock.Dispose();
        _inboxCapacityLimit.Dispose();
    }
}
