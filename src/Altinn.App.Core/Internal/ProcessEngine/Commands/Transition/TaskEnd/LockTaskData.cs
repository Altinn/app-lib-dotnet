using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal sealed class LockTaskData : IProcessEngineCommand
{
    public static string Key => "LockTaskData";

    public string GetKey() => Key;

    private readonly IProcessTaskDataLocker _processTaskDataLocker;

    public LockTaskData(IProcessTaskDataLocker processTaskDataLocker)
    {
        _processTaskDataLocker = processTaskDataLocker;
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        Instance instance = parameters.InstanceDataMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        try
        {
            await _processTaskDataLocker.Lock(taskId, instance);
            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
