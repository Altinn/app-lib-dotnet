namespace Altinn.App.Core.Internal.WorkflowEngine.Commands;

internal abstract class ProcessEngineCommandResult { }

internal sealed class SuccessfulProcessEngineCommandResult : ProcessEngineCommandResult { }

internal sealed class FailedProcessEngineCommandResult : ProcessEngineCommandResult
{
    public readonly string ErrorMessage;
    public readonly string ExceptionType;
    public readonly bool NonRetryable;

    /// <summary>
    /// Creates a retryable failure from a caught exception (likely transient — Storage down, HTTP timeout, etc.).
    /// </summary>
    public static FailedProcessEngineCommandResult Retryable(Exception exception) =>
        new(exception.Message, exception.GetType().Name, nonRetryable: false);

    /// <summary>
    /// Creates a retryable failure from a caught exception (likely transient — Storage down, HTTP timeout, etc.).
    /// </summary>
    public static FailedProcessEngineCommandResult Retryable(string errorMessage, string? exceptionType = null) =>
        new(errorMessage, exceptionType, nonRetryable: false);

    /// <summary>
    /// Creates a non-retryable failure (validation error, business rule violation, etc.).
    /// The workflow engine will stop retrying and mark the step as permanently failed.
    /// </summary>
    public static FailedProcessEngineCommandResult Permanent(string errorMessage, string? exceptionType = null) =>
        new(errorMessage, exceptionType, nonRetryable: true);

    private FailedProcessEngineCommandResult(string errorMessage, string? exceptionType, bool nonRetryable)
    {
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType ?? "Not specified";
        NonRetryable = nonRetryable;
    }
}
