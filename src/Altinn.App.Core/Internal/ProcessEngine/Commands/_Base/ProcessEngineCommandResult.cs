using Altinn.App.Core.Features.Process;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal abstract class ProcessEngineCommandResult { }

internal sealed class SuccessfulProcessEngineCommandResult : ProcessEngineCommandResult { }

internal sealed class FailedProcessEngineCommandResult : ProcessEngineCommandResult
{
    public readonly string ErrorMessage;
    public readonly string ExceptionType;

    public FailedProcessEngineCommandResult(Exception exception)
    {
        ErrorMessage = exception.Message;
        ExceptionType = exception.GetType().Name;
    }

    public FailedProcessEngineCommandResult(FailedOnTaskStartingHandlerResult failedOnTaskStartingHandlerResult)
    {
        ErrorMessage = failedOnTaskStartingHandlerResult.ErrorMessage;
        ExceptionType = failedOnTaskStartingHandlerResult.ExceptionType ?? "Not specified";
    }

    public FailedProcessEngineCommandResult(FailedOnTaskEndingHandlerResult failedOnTaskEndingHandlerResult)
    {
        ErrorMessage = failedOnTaskEndingHandlerResult.ErrorMessage;
        ExceptionType = failedOnTaskEndingHandlerResult.ExceptionType ?? "Not specified";
    }

    public FailedProcessEngineCommandResult(FailedOnProcessEndingHandlerResult failedOnEndingHandlerResult)
    {
        ErrorMessage = failedOnEndingHandlerResult.ErrorMessage;
        ExceptionType = failedOnEndingHandlerResult.ExceptionType ?? "Not specified";
    }

    public FailedProcessEngineCommandResult(FailedOnTaskAbandonHandlerResult failedOnEndingHandlerResult)
    {
        ErrorMessage = failedOnEndingHandlerResult.ErrorMessage;
        ExceptionType = failedOnEndingHandlerResult.ExceptionType ?? "Not specified";
    }

    public FailedProcessEngineCommandResult(string errorMessage, string? exceptionType)
    {
        ErrorMessage = errorMessage;
        ExceptionType = exceptionType ?? "Not specified";
    }
}
