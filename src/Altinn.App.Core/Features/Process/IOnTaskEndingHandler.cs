namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Hook interface for custom end task logic.
/// </summary>
/// <remarks>
/// <strong>IMPORTANT: Implementations MUST be idempotent - this hook may be retried on failure.</strong>
/// </remarks>
[ImplementableByApps]
public interface IOnTaskEndingHandler
{
    /// <summary>
    /// Determines whether the hook should run for the given task ID.
    /// </summary>
    /// <param name="taskId">The task ID to check.</param>
    /// <returns>True if the hook should run for this task; otherwise, false.</returns>
    public bool ShouldRunForTask(string taskId);

    /// <summary>
    /// Executes the end task hook logic.
    /// </summary>
    /// <param name="context">A context object with relevant parameters and data.</param>
    /// <returns>An end task result indicating success or failure.</returns>
    public Task<OnEndingHandlerResult> ExecuteAsync(OnTaskEndingHandlerContext context);
}

/// <summary>
/// Parameters for end task hook execution.
/// </summary>
public sealed class OnTaskEndingHandlerContext
{
    /// <summary>
    /// An instance data mutator that can be used to access and modify instance data. Changes made will be automatically saved if the hook execution is successful.
    /// </summary>
    public required IInstanceDataMutator InstanceDataMutator { get; init; }
}

/// <summary>
/// Base class for end task hook execution results.
/// </summary>
public abstract class OnEndingHandlerResult : HookResult { }

/// <summary>
/// Represents a successful end task hook execution.
/// </summary>
public sealed class SuccessfulOnEndingHandlerResult : OnEndingHandlerResult { }

/// <summary>
/// Represents a failed end task hook execution.
/// </summary>
public sealed class FailedOnTaskEndingHandlerResult : OnEndingHandlerResult
{
    /// <summary>
    /// Gets the error message describing why the hook failed.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the exception type name if the failure was caused by an exception.
    /// </summary>
    public string? ExceptionType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedOnTaskEndingHandlerResult"/> class from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public FailedOnTaskEndingHandlerResult(Exception exception)
    {
        ErrorMessage = exception.Message;
        ExceptionType = exception.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedOnTaskEndingHandlerResult"/> class with a custom error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exceptionType">Optional exception type name.</param>
    public FailedOnTaskEndingHandlerResult(string errorMessage, string? exceptionType = null)
    {
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType;
    }
}
