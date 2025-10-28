namespace Altinn.App.Api.Controllers;

internal abstract class ProcessEngineCallbackHandlerResult
{
}

internal sealed class SuccessfulProcessEngineCallbackHandlerResult : ProcessEngineCallbackHandlerResult
{
}

internal sealed class FailedProcessEngineCallbackHandlerResult : ProcessEngineCallbackHandlerResult
{
    public readonly Exception Exception;

    public FailedProcessEngineCallbackHandlerResult(Exception exception)
    {
        Exception = exception;
    }
}
