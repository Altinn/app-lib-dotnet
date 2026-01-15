namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

/// <summary>
/// Represents a command that can be executed by the Process Engine.
/// </summary>
/// <remarks>
/// <strong>IMPORTANT: All implementations MUST be idempotent - commands may be retried on failure.</strong>
/// </remarks>
internal interface IProcessEngineCommand
{
    string GetKey();

    Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters);
};
