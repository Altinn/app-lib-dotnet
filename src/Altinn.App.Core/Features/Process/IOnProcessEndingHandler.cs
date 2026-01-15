namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Hook interface for custom end process logic.
/// </summary>
/// <remarks>
/// <strong>IMPORTANT: Implementations MUST be idempotent - this hook may be retried on failure.</strong>
/// </remarks>
[ImplementableByApps]
public interface IOnProcessEndingHandler
{
    /// <summary>
    /// Executes the end process hook logic.
    /// </summary>
    /// <param name="context">A context object with relevant parameters and data.</param>
    /// <returns>An end task result indicating success or failure.</returns>
    public Task<OnProcessEndingHandlerResult> ExecuteAsync(OnProcessEndingHandlerContext context);
}

/// <summary>
/// Parameters for end process hook execution.
/// </summary>
public sealed class OnProcessEndingHandlerContext
{
    /// <summary>
    /// An instance data mutator that can be used to access and modify instance data. Changes made will be automatically saved if the hook execution is successful.
    /// </summary>
    public required IInstanceDataMutator InstanceDataMutator { get; init; }
}

/// <summary>
/// Base class for end process hook execution results.
/// </summary>
public abstract class OnProcessEndingHandlerResult : HookResult { }

/// <summary>
/// Represents a successful end process hook execution.
/// </summary>
public sealed class SuccessfulOnProcessEndingHandlerResult : OnProcessEndingHandlerResult { }

/// <summary>
/// Represents a failed end process hook execution.
/// </summary>
public sealed class FailedOnProcessEndingHandlerResult : OnProcessEndingHandlerResult
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
    public FailedOnProcessEndingHandlerResult(Exception exception)
    {
        ErrorMessage = exception.Message;
        ExceptionType = exception.GetType().Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedOnTaskEndingHandlerResult"/> class with a custom error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="exceptionType">Optional exception type name.</param>
    public FailedOnProcessEndingHandlerResult(string errorMessage, string? exceptionType = null)
    {
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType;
    }
}
