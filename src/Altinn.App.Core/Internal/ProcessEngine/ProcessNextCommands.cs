using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.ProcessEngine.Commands;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

internal sealed class ProcessNextCommands
{
    private readonly AppImplementationFactory _appImplementationFactory;

    public ProcessNextCommands(AppImplementationFactory appImplementationFactory)
    {
        _appImplementationFactory = appImplementationFactory;
    }

    public List<string> Generate(ProcessStateChange processStateChange)
    {
        if (processStateChange.Events == null || processStateChange.Events.Count == 0)
            return [];

        var sequence = new ProcessCommandSequence();

        foreach (InstanceEvent instanceEvent in processStateChange.Events)
        {
            if (!Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType eventType))
                continue;

            string? taskId = instanceEvent.ProcessInfo?.CurrentTask?.ElementId;
            string? altinnTaskType = instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType;

            switch (eventType)
            {
                case InstanceEventType.process_EndTask:
                    sequence.AddEventCommands(TaskEndCommands(taskId));
                    break;

                case InstanceEventType.process_AbandonTask:
                    sequence.AddEventCommands(TaskAbandonCommands(taskId));
                    break;

                case InstanceEventType.process_StartTask:
                    sequence.AddEventCommands(TaskStartCommands(taskId));

                    // If this is a service task, add ExecuteServiceTask command
                    if (IsServiceTask(altinnTaskType))
                    {
                        sequence.AddCommand(ExecuteServiceTask.Key);
                    }
                    break;

                case InstanceEventType.process_EndEvent:
                    sequence.AddEventCommands(ProcessEndCommands(taskId));
                    break;
            }
        }

        return sequence.ToList();
    }

    private static List<string> TaskStartCommands(string? taskId)
    {
        return
        [
            UnlockTaskData.Key,
            StartTaskLegacyHook.Key,
            OnTaskStartingHook.Key,
            CommonTaskInitialization.Key,
            ProcessTaskStart.Key,
        ];
    }

    private static List<string> TaskEndCommands(string? taskId)
    {
        return
        [
            ProcessTaskEnd.Key,
            CommonTaskFinalization.Key,
            EndTaskLegacyHook.Key,
            OnTaskEndingHook.Key,
            LockTaskData.Key,
        ];
    }

    private static List<string> TaskAbandonCommands(string? taskId)
    {
        return [ProcessTaskAbandon.Key, OnTaskAbandonHook.Key, AbandonTaskLegacyHook.Key];
    }

    private static List<string> ProcessEndCommands(string? taskId)
    {
        return [OnProcessEndingHook.Key, ProcessEndLegacyHook.Key];
    }

    private bool IsServiceTask(string? altinnTaskType)
    {
        if (altinnTaskType is null)
            return false;

        IEnumerable<IServiceTask> serviceTasks = _appImplementationFactory.GetAll<IServiceTask>();
        return serviceTasks.Any(x => x.Type.Equals(altinnTaskType, StringComparison.OrdinalIgnoreCase));
    }
}
