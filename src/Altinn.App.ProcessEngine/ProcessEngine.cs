using System.Threading.Channels;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.ProcessEngine;

/// <summary>
/// The status of a request to the process engine.
/// </summary>
public enum ProcessEngineRequestStatus
{
    Accepted,
    Rejected,
}

/// <summary>
/// A request to enqueue one or more task in the process engine.
/// </summary>
public sealed record ProcessEngineRequest(
    AppIdentifier AppIdentifier,
    Instance Instance,
    IEnumerable<ProcessEngineTaskRequest> Tasks
)
{
    // TODO: Implement some basic validation here
    public bool IsValid() => Tasks.Any();
};

/// <summary>
/// Represents a single task to be processed by the process engine.
/// </summary>
public sealed record ProcessEngineTaskRequest(
    string Identifier,
    ProcessEngineTaskInstruction Instruction,
    DateTimeOffset? StartTime = null
);

/// <summary>
/// The response from the process engine for a <see cref="ProcessEngineRequest"/>.
/// </summary>
public sealed record ProcessEngineResponse(ProcessEngineRequestStatus Status, string? Message = null)
{
    public static ProcessEngineResponse Accepted(string? message = null) =>
        new(ProcessEngineRequestStatus.Accepted, message);

    public static ProcessEngineResponse Rejected(string? message = null) =>
        new(ProcessEngineRequestStatus.Rejected, message);
};

public abstract record ProcessEngineTaskInstruction
{
    private ProcessEngineTaskInstruction() { }

    public sealed record MoveProcessForward(string From, string To, string? Action = null)
        : ProcessEngineTaskInstruction;

    public sealed record ExecuteServiceTask(string Identifier) : ProcessEngineTaskInstruction;

    public sealed record ExecuteInterfaceHooks(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendCorrespondence(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendEformidling(object Something) : ProcessEngineTaskInstruction;

    public sealed record SendFiksArkiv(object Something) : ProcessEngineTaskInstruction;

    public sealed record PublishAltinnEvent(object Something) : ProcessEngineTaskInstruction;
}

// [Flags]
// internal enum ProcessEngineItemStatus
// {
//     Enqueued = 0,
//     Processing = 1 << 0,
//     Requeued = 1 << 1,
//     Completed = 1 << 2,
//     Failed = 1 << 3,
//     Canceled = 1 << 4,
// }
internal enum ProcessEngineItemStatus
{
    Enqueued = 0,
    Processing = 1,
    Requeued = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5,
}

internal static class ProcessEngineItemStatusExtensions
{
    public static bool IsDone(this ProcessEngineItemStatus status) =>
        status
            is ProcessEngineItemStatus.Completed
                or ProcessEngineItemStatus.Failed
                or ProcessEngineItemStatus.Canceled;
}

internal sealed record ProcessEngineJob
{
    public ProcessEngineItemStatus Status { get; set; }
    public required AppIdentifier AppIdentifier { get; init; }
    public required Instance Instance { get; init; }
    public required List<ProcessEngineTask> Tasks { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;

    public static ProcessEngineJob FromRequest(ProcessEngineRequest request) =>
        new()
        {
            AppIdentifier = request.AppIdentifier,
            Instance = request.Instance,
            Tasks = request.Tasks.Select(ProcessEngineTask.FromRequest).ToList(),
        };
};

internal sealed record ProcessEngineTask
{
    public ProcessEngineItemStatus Status { get; set; }
    public required string Identifier { get; init; }
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineTaskInstruction Instruction { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public int RequeueCount { get; set; }

    public static ProcessEngineTask FromRequest(ProcessEngineTaskRequest request, int index) =>
        new()
        {
            Identifier = request.Identifier,
            StartTime = request.StartTime,
            ProcessingOrder = index,
            Instruction = request.Instruction,
        };
};

internal static class ProcessEngineJobExtensions
{
    public static IOrderedEnumerable<ProcessEngineTask> OrderedTasks(this ProcessEngineJob job) =>
        job.Tasks.OrderBy(t => t.ProcessingOrder);

    public static IOrderedEnumerable<ProcessEngineTask> OrderedIncompleteTasks(this ProcessEngineJob job) =>
        job.Tasks.Where(x => !x.Status.IsDone()).OrderBy(x => x.ProcessingOrder);
}

internal interface IProcessEngine
{
    Task Start(CancellationToken cancellationToken = default);
    Task Stop();
    Task<ProcessEngineResponse> EnqueueJob(ProcessEngineRequest request, CancellationToken cancellationToken = default);
}

internal sealed class ProcessEngine : IProcessEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<ProcessEngineJob> _inbox;
    private readonly ProcessEngineSettings _settings;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _cancellationTokenSource;

    // private TaskCompletionSource<bool>? _mainLoopTaskAwaiter;
    private Task? _mainLoopTask;

    // private Task? _cleanupTask;
    private bool? _lastShouldRunResult;
    private readonly IProcessEngineTaskHandler _taskHandler;

    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private volatile bool _cleanupRequired;

    public ProcessEngine(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _taskHandler = serviceProvider.GetRequiredService<IProcessEngineTaskHandler>();
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;
        _inbox = Channel.CreateBounded<ProcessEngineJob>(
            new BoundedChannelOptions(_settings.QueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        if (_cancellationTokenSource is not null || _mainLoopTask is not null)
            await Stop();

        _cleanupRequired = true;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _mainLoopTask = Task.Run(
            async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        await MainLoop(_cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    await Cleanup();
                }
            },
            _cancellationTokenSource.Token
        );
    }

    public async Task Stop()
    {
        // Already stopped
        if (_cancellationTokenSource is null || _cancellationTokenSource.IsCancellationRequested)
            return;

        try
        {
            await _cancellationTokenSource.CancelAsync();

            if (_mainLoopTask?.IsCompleted is false)
                await _mainLoopTask;
        }
        catch (OperationCanceledException) { }
    }

    public async Task<ProcessEngineResponse> EnqueueJob(
        ProcessEngineRequest request,
        CancellationToken cancellationToken = default
    )
    {
        if (!request.IsValid())
            return ProcessEngineResponse.Rejected("Invalid task request");

        _lastShouldRunResult ??= await ShouldRun(cancellationToken);
        if (_lastShouldRunResult is false)
            return ProcessEngineResponse.Rejected(
                "ProcessEngine is currently inactive. Did you call the right instance?"
            );

        // TODO: Duplication check? If we already have an active job with some form of calculated ID, disallow enqueue

        var taskCollection = ProcessEngineJob.FromRequest(request);
        await EnqueueJob(taskCollection, true, cancellationToken);

        return ProcessEngineResponse.Accepted();
    }

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
        // TODO: This must be a resilient call to db
        return Task.CompletedTask;
    }

    private Task UpdateTaskInStorage(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        // TODO: This must be a resilient call to db
        return Task.CompletedTask;
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
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    private void TrackShouldRunResult(bool result)
    {
        if (answer is false)
            LastAnswerWasNo = true;
        else
            LastAnswerWasNo = false;
    }

    private async Task<bool> ShouldRun(CancellationToken cancellationToken)
    {
        // E.g. "do we hold the lock?"

        // TODO: Implement logic to determine if the process engine should run
        // TODO: Some sort of progressive backoff if previous answer was "no"

        await Task.Delay(100, cancellationToken);

        _lastShouldRunResult = true;

        if (_lastShouldRunResult is true && LastAnswerWasNo)
        {
            await PopulateJobsFromStorage(cancellationToken);
        }

        return _lastShouldRunResult.Value;
    }

    private async Task MainLoop(CancellationToken cancellationToken)
    {
        // Should we run?
        if (!await ShouldRun(cancellationToken))
            return;

        // Trackers for reentry
        var unprocessableJobs = new List<ProcessEngineJob>();
        var requeueJobs = new List<ProcessEngineJob>();

        // Loop over queue
        while (_inbox.Reader.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var job = DequeueJob();
            if (job is null)
                break;

            // No more tasks to process, mark job as completed
            if (!job.OrderedIncompleteTasks().Any())
            {
                await UpdateJobInStorage(job with { Status = ProcessEngineItemStatus.Completed }, cancellationToken);
                break;
            }

            // TODO: Might need more parallelism here. One thread per instance? Per job?
            // TODO: PS tasks cannot be concurrent, they must be sequential
            foreach (var task in job.OrderedIncompleteTasks())
            {
                // Not time to process yet
                if (task.StartTime.HasValue && task.StartTime > _timeProvider.GetUtcNow())
                {
                    unprocessableJobs.Add(job);
                    break;
                }

                var result = await _taskHandler.Execute(task, cancellationToken);
                if (result.IsSuccess())
                {
                    task.Status = ProcessEngineItemStatus.Completed;
                    await UpdateTaskInStorage(task, cancellationToken);
                    continue;
                }

                // TODO: Logic to determine if we should requeue or fail
                // TODO: Progressive backoff delay?
                // TODO: If we fail, also fail the entire job
                task.Status = ProcessEngineItemStatus.Requeued;
                task.RequeueCount++;

                await UpdateTaskInStorage(task, cancellationToken);
                requeueJobs.Add(job);
                break;
            }

            // Update job status
            if (job.Status != ProcessEngineItemStatus.Failed)
            {
                job.Status = job.OrderedIncompleteTasks().Any()
                    ? ProcessEngineItemStatus.Requeued
                    : ProcessEngineItemStatus.Completed;
            }

            await UpdateJobInStorage(job, cancellationToken);
        }

        // Requeue jobs that had failed tasks. Task status is already updated in db
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
}
