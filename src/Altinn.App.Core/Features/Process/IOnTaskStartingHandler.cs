namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Hook interface for custom start task logic.
/// </summary>
[ImplementableByApps]
public interface IOnTaskStartingHandler
{
    /// <summary>
    /// Determines whether the hook should run for the given task ID.
    /// </summary>
    /// <param name="taskId">The task ID to check.</param>
    /// <returns>True if the hook should run for this task; otherwise, false.</returns>
    public bool ShouldRunForTask(string taskId);

    /// <summary>
    /// Executes the start task hook logic.
    /// </summary>
    /// <param name="context">A context object with relevant parameters and data.</param>
    /// <returns>A start task result indicating success or failure.</returns>
    public Task<OnTaskStartingHandlerResult> ExecuteAsync(OnTaskStartingContext context);
}

/// <summary>
/// Parameters for start task hook execution.
/// </summary>
public sealed class OnTaskStartingContext
{
    /// <summary>
    /// An instance data mutator that can be used to access and modify instance data. Changes made will be automatically saved if the hook execution is successful.
    /// </summary>
    public required IInstanceDataMutator InstanceDataMutator { get; init; }
}

/// <summary>
/// Base class for start task hook execution results.
/// </summary>
public abstract class OnTaskStartingHandlerResult : TaskHookResult { }

/// <summary>
/// Represents a successful start task hook execution.
/// </summary>
public sealed class SuccessfulOnTaskStartingHandlerResult : OnTaskStartingHandlerResult { }

/// <summary>
/// Represents a failed start task hook execution.
/// </summary>
public sealed class FailedOnTaskStartingHandlerResult : OnTaskStartingHandlerResult
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
    /// Initializes a new instance of the <see cref="FailedOnTaskStartingHandlerResult"/> class from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public FailedOnTaskStartingHandlerResult(Exception exception)
    {
        ErrorMessage = exception.Message;
        ExceptionType = exception.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedOnTaskStartingHandlerResult"/> class with a custom error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exceptionType">Optional exception type name.</param>
    public FailedOnTaskStartingHandlerResult(string errorMessage, string? exceptionType = null)
    {
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType;
    }
}
