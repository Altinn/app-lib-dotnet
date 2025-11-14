using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands.Transition.TaskAbandon;

internal sealed class OnAbandonTask : IProcessEngineCommand
{
    public static string Key => "OnAbandonTask";

    public string GetKey() => Key;

    private readonly ProcessTaskResolver _processTaskResolver;

    public OnAbandonTask(ProcessTaskResolver processTaskResolver)
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
            await processTask.Abandon(dataMutator);
            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
