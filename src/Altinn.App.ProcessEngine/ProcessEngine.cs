using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.ProcessEngine;

internal interface IProcessEngine
{
    ProcessEngineHealthStatus Status { get; }
    Task Start(CancellationToken cancellationToken = default);
    Task Stop();
    Task<ProcessEngineResponse> EnqueueJob(ProcessEngineRequest request, CancellationToken cancellationToken = default);
}

// TODO: This is already a big boi. Partial classes? More services?

internal sealed class ProcessEngine : IProcessEngine, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProcessEngineSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly IProcessEngineTaskHandler _taskHandler;
    private readonly Buffer<bool> _enabledStatusHistory = new();
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private readonly ProcessEngineRetryStrategy _statusCheckBackoffStrategy = new(
        ProcessEngineBackoffType.Exponential,
        Delay: TimeSpan.FromSeconds(1)
    );

    private Channel<ProcessEngineJob> _inbox;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _mainLoopTask;
    private SemaphoreSlim _inboxCapacityLimit;
    private volatile bool _cleanupRequired;
    private bool _disposed;

    // TODO: Is this overengineered? Maybe we only care if it's running or not?
    public ProcessEngineHealthStatus Status { get; private set; } = ProcessEngineHealthStatus.Healthy;

    public ProcessEngine(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ProcessEngine>>();
        _taskHandler = serviceProvider.GetRequiredService<IProcessEngineTaskHandler>();
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;

        InitializeInbox();
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
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
        if (!request.IsValid())
            return ProcessEngineResponse.Rejected("Invalid request");

        if (_mainLoopTask is null)
            return ProcessEngineResponse.Rejected("Process engine is not running. Did you call Start()?");

        var enabled = await _enabledStatusHistory.Latest() ?? await ShouldRun(cancellationToken);
        if (!enabled)
            return ProcessEngineResponse.Rejected(
                "ProcessEngine is currently inactive. Did you call the right instance?"
            );

        // TODO: Duplication check? If we already have an active job with some form of calculated ID, disallow enqueue

        await AcquireQueueSlot(cancellationToken); // Only acquire slots for public requests
        await EnqueueJob(ProcessEngineJob.FromRequest(request), true, cancellationToken);

        return ProcessEngineResponse.Accepted();
    }

    private async Task AcquireQueueSlot(CancellationToken cancellationToken = default)
    {
        await _inboxCapacityLimit.WaitAsync(cancellationToken);

        if (_inbox.Reader.Count >= _settings.QueueCapacity)
            Status |= ProcessEngineHealthStatus.QueueFull;
        else
            Status &= ~ProcessEngineHealthStatus.QueueFull;
    }

    private void ReleaseQueueSlot() => _inboxCapacityLimit.Release();

    private async Task EnqueueJob(
        ProcessEngineJob job,
        bool updateDatabase = true,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: persist to database if `updateDatabase` is true
        await _inbox.Writer.WriteAsync(job, cancellationToken);
    }

    private ProcessEngineJob? DequeueJob()
    {
        // TODO: Do we persist this checkout in the db? Probably not
        return _inbox.Reader.TryRead(out var job) ? job : null;
    }

    private Task PopulateJobsFromStorage(CancellationToken cancellationToken)
    {
        // TODO: Populate the queue from the database. This must be a resilient call to db
        return Task.CompletedTask;
    }

    private Task UpdateJobInStorage(ProcessEngineJob job, CancellationToken cancellationToken)
    {
        // TODO: Should we update the `Instance` with something here too? Like if the job has failed, etc
        // TODO: This must be a resilient call to db
        return Task.CompletedTask;
    }

    private Task UpdateTaskInStorage(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        // TODO: This must be a resilient call to db
        return Task.CompletedTask;
    }

    private async Task<bool> ShouldRun(CancellationToken cancellationToken)
    {
        // E.g. "do we hold the lock?"

        // TODO: Implement logic to determine if the process engine should run
        // TODO: Some sort of progressive backoff if previous answer was "no"

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
        {
            await PopulateJobsFromStorage(cancellationToken);
        }

        // Progressive backoff we have been disabled for two or more consecutive checks
        if (latest is false && previous is false)
        {
            int iteration = await _enabledStatusHistory.ConsecutiveCount(x => !x);
            var backoffDelay = _statusCheckBackoffStrategy.CalculateDelay(iteration);
            await Task.Delay(backoffDelay, cancellationToken);
        }

        return placeholderEnabledResponse;
    }

    [MemberNotNull(nameof(_inbox), nameof(_inboxCapacityLimit))]
    private void InitializeInbox()
    {
        _inbox = Channel.CreateUnbounded<ProcessEngineJob>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );
        _inboxCapacityLimit = new SemaphoreSlim(_settings.QueueCapacity, _settings.QueueCapacity);
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

            _inbox.Writer.Complete();
            _inboxCapacityLimit.Dispose();

            InitializeInbox();
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    private async Task MainLoop(CancellationToken cancellationToken)
    {
        // Should we run?
        if (!await ShouldRun(cancellationToken))
        {
            Status |= ProcessEngineHealthStatus.Disabled;
            return;
        }

        // Trackers for reentry
        var unprocessableJobs = new List<ProcessEngineJob>();
        var requeueJobs = new List<ProcessEngineJob>();

        // Loop over queue
        while (_inbox.Reader.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var job = DequeueJob();
            if (job is null)
                break;

            bool jobIsUnprocessable = false;

            // TODO: Might need more parallelism here. One thread per instance? Per job?
            // TODO: PS tasks cannot be concurrent, they must be sequential
            foreach (var task in job.OrderedIncompleteTasks())
            {
                // Not time to process yet
                if (task.IsReadyForExecution(_timeProvider))
                {
                    jobIsUnprocessable = true;
                    break;
                }

                // Already processing and not finished
                if (task.ExecutionTask?.IsCompleted is false)
                {
                    jobIsUnprocessable = true;
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
                    task.Status = ProcessEngineItemStatus.Processing; // Don't persist this, we're dependent on the in-memory task anyway
                    break;
                }

                // Foreground wait
                var result = await task.ExecutionTask;
                if (result.IsSuccess())
                {
                    task.ExecutionTask.Dispose();
                    task.Status = ProcessEngineItemStatus.Completed;
                    await UpdateTaskInStorage(task, cancellationToken);
                    continue;
                }

                // Error handling
                _logger.LogError("Task {Task} failed: {ErrorMessage}", task, result.Message);
                task.RequeueCount++;
                task.ExecutionTask.Dispose();
                task.ExecutionTask = null;

                var retryStrategy = task.RetryStrategy ?? _settings.DefaultTaskRetryStrategy;
                if (retryStrategy.CanRetry(task.RequeueCount))
                {
                    task.Status = ProcessEngineItemStatus.Requeued;
                    task.BackoffUntil = _timeProvider.GetUtcNow().Add(retryStrategy.CalculateDelay(task.RequeueCount));
                }
                else
                {
                    task.Status = ProcessEngineItemStatus.Failed;
                }

                await UpdateTaskInStorage(task, cancellationToken);
                break;
            }

            // Job is unprocessable
            if (jobIsUnprocessable)
            {
                unprocessableJobs.Add(job);
                continue;
            }

            // Job needs requeue or is done
            job.Status = job.OverallStatus();
            await UpdateJobInStorage(job, cancellationToken);

            if (job.Status.IsDone())
                ReleaseQueueSlot();
            else
                requeueJobs.Add(job);
        }

        // Requeue jobs that had failed tasks first
        foreach (var job in requeueJobs)
        {
            await EnqueueJob(job, updateDatabase: false, cancellationToken);
        }

        // Requeue jobs that were not ready to be processed
        foreach (var job in unprocessableJobs)
        {
            await EnqueueJob(job, updateDatabase: false, cancellationToken);
        }

        // Small delay before next iteration
        await Task.Delay(100, cancellationToken);
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
