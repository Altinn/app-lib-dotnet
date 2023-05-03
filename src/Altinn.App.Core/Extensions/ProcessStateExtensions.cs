using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="ProcessState"/>
/// </summary>
public static class ProcessStateExtensions
{
    public static ProcessState Copy(this ProcessState original)
    {
        ProcessState copyOfState = new ProcessState();

        if (original.CurrentTask != null)
        {
            copyOfState.CurrentTask = new ProcessElementInfo();
            copyOfState.CurrentTask.FlowType = original.CurrentTask.FlowType;
            copyOfState.CurrentTask.Name = original.CurrentTask.Name;
            copyOfState.CurrentTask.Validated = original.CurrentTask.Validated;
            copyOfState.CurrentTask.AltinnTaskType = original.CurrentTask.AltinnTaskType;
            copyOfState.CurrentTask.Flow = original.CurrentTask.Flow;
            copyOfState.CurrentTask.ElementId = original.CurrentTask.ElementId;
            copyOfState.CurrentTask.Started = original.CurrentTask.Started;
            copyOfState.CurrentTask.Ended = original.CurrentTask.Ended;
        }

        copyOfState.EndEvent = original.EndEvent;
        copyOfState.Started = original.Started;
        copyOfState.Ended = original.Ended;
        copyOfState.StartEvent = original.StartEvent;

        return copyOfState;
    } 
}