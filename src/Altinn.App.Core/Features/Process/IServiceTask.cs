using Altinn.App.Core.Internal.Process.ProcessTasks;

namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Interface for service tasks that can be executed during a process.
/// </summary>
/// <remarks>
/// <strong>IMPORTANT: Implementations MUST be idempotent - service tasks may be retried on failure.</strong>
/// </remarks>
[ImplementableByApps]
public interface IServiceTask : IProcessTask
{
    /// <summary>
    /// Executes the service task.
    /// </summary>
    public Task<ServiceTaskResult> Execute(ServiceTaskContext context);
}

/// <summary>
/// This class represents the parameters for executing a service task.
/// </summary>
public sealed record ServiceTaskContext
{
    /// <summary>
    /// An instance data mutator that can be used to read and modify the instance data during the service task execution.
    /// </summary>
    /// <remarks>Changes are saved after Execute returns a successful result. Keep in mind that data elements from previous tasks are locked.</remarks>
    public required IInstanceDataMutator InstanceDataMutator { get; init; }

    /// <summary>
    /// Cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}

/// <summary>
/// Base type for the result of executing a service task.
/// </summary>
public abstract record ServiceTaskResult
{
    /// <summary>
    /// Creates a service task result representing successful execution.
    /// </summary>
    public static ServiceTaskSuccessResult Success() => new();

    /// <summary>
    /// Creates a retryable failure. The workflow engine will retry the step with backoff.
    /// Use this for transient errors (external service down, timeout, rate limit, etc.).
    /// </summary>
    /// <param name="errorMessage">Human-readable error message describing the failure.</param>
    public static ServiceTaskFailedResult FailedRetryable(string errorMessage) =>
        new(errorMessage, NonRetryable: false);

    /// <summary>
    /// Creates a permanent (non-retryable) failure. The workflow engine will stop retrying
    /// and mark the step as failed immediately.
    /// Use this for errors that won't resolve by retrying (validation failure, missing config, bad data, etc.).
    /// </summary>
    /// <param name="errorMessage">Human-readable error message describing the failure.</param>
    public static ServiceTaskFailedResult FailedPermanent(string errorMessage) => new(errorMessage, NonRetryable: true);
}

/// <summary>
/// Represents a successful result of executing a service task.
/// </summary>
public sealed record ServiceTaskSuccessResult : ServiceTaskResult;

/// <summary>
/// Represents a failed result of executing a service task.
/// </summary>
/// <param name="ErrorMessage">Human-readable error message describing the failure.</param>
/// <param name="NonRetryable">
/// If true, the workflow engine will not retry this step (permanent failure).
/// If false, the workflow engine will retry with backoff (transient failure).
/// </param>
public sealed record ServiceTaskFailedResult(string ErrorMessage, bool NonRetryable) : ServiceTaskResult;
