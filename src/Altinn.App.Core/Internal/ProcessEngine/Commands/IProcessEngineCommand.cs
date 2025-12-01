namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal interface IProcessEngineCommand
{
    string GetKey();

    Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters);
};
