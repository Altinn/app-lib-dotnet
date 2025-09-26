namespace Altinn.App.ProcessEngine.Models;

internal record struct ProcessEngineExecutionResult(ProcessEngineExecutionStatus Status, string? Message = null);
