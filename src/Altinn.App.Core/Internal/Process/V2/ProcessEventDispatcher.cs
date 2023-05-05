using Altinn.App.Core.Configuration;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.V2;

class ProcessEventDispatcher : IProcessEventDispatcher
{
    private readonly IInstance _instanceService;
    private readonly IProcess _processClient;
    private readonly ITaskEvents _taskEvents;
    private readonly IAppEvents _appEvents;
    private readonly IEvents _eventsService;
    private readonly bool _registerWithEventSystem;
    private ILogger<ProcessEventDispatcher> _logger;

    public ProcessEventDispatcher(
        IInstance instanceService, 
        IProcess processClient, 
        ITaskEvents taskEvents, 
        IAppEvents appEvents, 
        IEvents eventsService, 
        IOptions<AppSettings> appSettings,
        ILogger<ProcessEventDispatcher> logger)
    {
        _instanceService = instanceService;
        _processClient = processClient;
        _taskEvents = taskEvents;
        _appEvents = appEvents;
        _eventsService = eventsService;
        _registerWithEventSystem = appSettings.Value.RegisterEventsWithEventsComponent;
        _logger = logger;
    }

    public async Task<Instance> UpdateProcessAndDispatchEvents(Instance instance, Dictionary<string, string>? prefill, List<InstanceEvent> events)
    {
        await HandleProcessChanges(instance, events, prefill);

        // need to update the instance process and then the instance in case appbase has changed it, e.g. endEvent sets status.archived
        Instance updatedInstance = await _instanceService.UpdateProcess(instance);
        await _processClient.DispatchProcessEventsToStorage(updatedInstance, events);

        // remember to get the instance anew since AppBase can have updated a data element or stored something in the database.
        updatedInstance = await _instanceService.GetInstance(updatedInstance);

        return updatedInstance;
    }

    public async Task RegisterEventWithEventsComponent(Instance instance)
    {
        if (_registerWithEventSystem)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(instance.Process.CurrentTask?.ElementId))
                {
                    await _eventsService.AddEvent($"app.instance.process.movedTo.{instance.Process.CurrentTask.ElementId}", instance);
                }
                else if (instance.Process.EndEvent != null)
                {
                    await _eventsService.AddEvent("app.instance.process.completed", instance);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Exception when sending event with the Events component");
            }
        }
    }

    /// <summary>
    /// Will for each process change trigger relevant Process Elements to perform the relevant change actions.
    ///
    /// Each implementation 
    /// </summary>
    private async Task HandleProcessChanges(Instance instance, List<InstanceEvent> events, Dictionary<string, string>? prefill)
    {
        foreach (InstanceEvent processEvent in events)
        {
            if (Enum.TryParse<InstanceEventType>(processEvent.EventType, true, out InstanceEventType eventType))
            {
                string? elementId = processEvent.ProcessInfo?.CurrentTask?.ElementId;
                ITask task = GetProcessTask(processEvent.ProcessInfo?.CurrentTask?.AltinnTaskType);
                switch (eventType)
                {
                    case InstanceEventType.process_StartEvent:
                        break;
                    case InstanceEventType.process_StartTask:
                        await task.HandleTaskStart(elementId, instance, prefill);
                        break;
                    case InstanceEventType.process_EndTask:
                        await task.HandleTaskComplete(elementId, instance);
                        break;
                    case InstanceEventType.process_AbandonTask:
                        await task.HandleTaskAbandon(elementId, instance);
                        await _instanceService.UpdateProcess(instance);
                        break;
                    case InstanceEventType.process_EndEvent:
                        await _appEvents.OnEndAppEvent(processEvent.ProcessInfo?.EndEvent, instance);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Identify the correct task implementation
    /// </summary>
    /// <returns></returns>
    private ITask GetProcessTask(string? altinnTaskType)
    {
        if (string.IsNullOrEmpty(altinnTaskType))
        {
            return new NullTask();
        }

        ITask task = new DataTask(_taskEvents);
        if (altinnTaskType.Equals("confirmation"))
        {
            task = new ConfirmationTask(_taskEvents);
        }
        else if (altinnTaskType.Equals("feedback"))
        {
            task = new FeedbackTask(_taskEvents);
        }

        return task;
    }
}