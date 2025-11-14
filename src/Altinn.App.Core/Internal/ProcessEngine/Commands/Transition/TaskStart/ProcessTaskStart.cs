using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands.Transition.TaskStart;

internal sealed class ProcessTaskStart : IProcessEngineCommand
{
    public static string Key => "ProcessTaskStart";

    public string GetKey() => Key;

    private readonly ProcessTaskResolver _processTaskResolver;

    public ProcessTaskStart(ProcessTaskResolver processTaskResolver)
    {
        _processTaskResolver = processTaskResolver;
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        IInstanceDataMutator dataMutator = parameters.InstanceDataMutator;
        string? altinnTaskType = dataMutator.Instance.Process.CurrentTask.AltinnTaskType;

        try
        {
            IProcessTask processTask = _processTaskResolver.GetProcessTaskInstance(altinnTaskType);
            await processTask.Start(dataMutator);
            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
