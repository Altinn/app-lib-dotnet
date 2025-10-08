namespace Altinn.App.ProcessEngine.Models;

internal record struct ProcessEngineExecutionResult(ProcessEngineExecutionStatus Status, string? Message = null)
{
    public static ProcessEngineExecutionResult Success() => new(ProcessEngineExecutionStatus.Success);

    public static ProcessEngineExecutionResult Error(string message) =>
        new(ProcessEngineExecutionStatus.Error, message);
};
