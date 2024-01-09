using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Internal.Process.TaskTypes;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Default implementation of the process event dispatcher
/// </summary>
class ProcessEventDispatcher : IProcessEventDispatcher
{
    private readonly IInstanceClient _instanceClient;
    private readonly IInstanceEventClient _instanceEventClient;
    private readonly IAppEvents _appEvents;
    private readonly IEventsClient _eventsClient;
    private readonly bool _registerWithEventSystem;
    private readonly IEnumerable<IProcessTaskType> _processTaskTypes;
    private readonly IEnumerable<IProcessTaskStart> _taskStarts;
    private readonly IEnumerable<IProcessTaskEnd> _taskEnds;
    private readonly IEnumerable<IProcessTaskAbandon> _taskAbandons;
    private readonly ProcessTaskStartCommonLogic _startCommonLogic;
    private readonly ProcessTaskEndCommonLogic _endCommonLogic;
    private readonly ProcessTaskLockingCommonLogic _lockingCommonLogic;
    private readonly ILogger<ProcessEventDispatcher> _logger;
    private readonly PdfServiceTask _pdfServiceTask;
    private readonly EformidlingServiceTask _eformidlingServiceTask;
    private readonly IAppMetadata _appMetadata;

    public ProcessEventDispatcher(
        IInstanceClient instanceClient,
        IInstanceEventClient instanceEventClient,
        IAppEvents appEvents,
        IEventsClient eventsClient,
        IOptions<AppSettings> appSettings,
        IEnumerable<IProcessTaskType> processTaskTypes,
        ILogger<ProcessEventDispatcher> logger,
        IEnumerable<IProcessTaskStart> taskStarts,
        IEnumerable<IProcessTaskEnd> taskEnds,
        IEnumerable<IProcessTaskAbandon> taskAbandons,
        PdfServiceTask pdfServiceTask,
        EformidlingServiceTask eformidlingServiceTask,
        ProcessTaskStartCommonLogic startCommonLogic,
        ProcessTaskEndCommonLogic endCommonLogic,
        ProcessTaskLockingCommonLogic lockingCommonLogic, IAppMetadata appMetadata)
    {
        _instanceClient = instanceClient;
        _instanceEventClient = instanceEventClient;
        _appEvents = appEvents;
        _eventsClient = eventsClient;
        _registerWithEventSystem = appSettings.Value.RegisterEventsWithEventsComponent;
        _logger = logger;
        _taskStarts = taskStarts;
        _taskEnds = taskEnds;
        _taskAbandons = taskAbandons;
        _pdfServiceTask = pdfServiceTask;
        _eformidlingServiceTask = eformidlingServiceTask;
        _startCommonLogic = startCommonLogic;
        _endCommonLogic = endCommonLogic;
        _lockingCommonLogic = lockingCommonLogic;
        _appMetadata = appMetadata;
        _processTaskTypes = processTaskTypes;
    }

    /// <inheritdoc/>
    public async Task<Instance> UpdateProcessAndDispatchEvents(Instance instance, Dictionary<string, string>? prefill,
        List<InstanceEvent>? events)
    {
        await HandleProcessChanges(instance, events, prefill);

        // need to update the instance process and then the instance in case appbase has changed it, e.g. endEvent sets status.archived
        Instance updatedInstance = await _instanceClient.UpdateProcess(instance);
        await DispatchProcessEventsToStorage(updatedInstance, events);

        // remember to get the instance anew since AppBase can have updated a data element or stored something in the database.
        updatedInstance = await _instanceClient.GetInstance(updatedInstance);

        return updatedInstance;
    }

    /// <inheritdoc/>
    public async Task RegisterEventWithEventsComponent(Instance instance)
    {
        if (_registerWithEventSystem)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(instance.Process.CurrentTask?.ElementId))
                {
                    await _eventsClient.AddEvent(
                        $"app.instance.process.movedTo.{instance.Process.CurrentTask.ElementId}", instance);
                }
                else if (instance.Process.EndEvent != null)
                {
                    await _eventsClient.AddEvent("app.instance.process.completed", instance);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Exception when sending event with the Events component");
            }
        }
    }


    private async Task DispatchProcessEventsToStorage(Instance instance, List<InstanceEvent>? events)
    {
        string org = instance.Org;
        string app = instance.AppId.Split("/")[1];

        if (events != null)
        {
            foreach (InstanceEvent instanceEvent in events)
            {
                instanceEvent.InstanceId = instance.Id;
                await _instanceEventClient.SaveInstanceEvent(instanceEvent, org, app);
            }
        }
    }

    /// <summary>
    /// Will for each process change trigger relevant Process Elements to perform the relevant change actions.
    ///
    /// Each implementation 
    /// </summary>
    private async Task HandleProcessChanges(Instance instance, List<InstanceEvent>? events,
        Dictionary<string, string>? prefill)
    {
        if (events != null)
        {
            foreach (InstanceEvent instanceEvent in events)
            {
                if (Enum.TryParse<InstanceEventType>(instanceEvent.EventType, true, out InstanceEventType eventType))
                {
                    string? elementId = instanceEvent.ProcessInfo?.CurrentTask?.ElementId;
                    IProcessTaskType processTaskType =
                        GetProcessTask(instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType);
                    switch (eventType)
                    {
                        case InstanceEventType.process_StartEvent:
                            break;
                        case InstanceEventType.process_StartTask:
                            await _lockingCommonLogic.UnlockConnectedDataTypes(elementId, instance);
                            await RunAppDefinedOnTaskStart(elementId, instance, prefill);
                            await _startCommonLogic.Start(elementId, instance, prefill);
                            await processTaskType.HandleTaskStart(elementId, instance, prefill);
                            break;
                        case InstanceEventType.process_EndTask:
                            await processTaskType.HandleTaskComplete(elementId, instance);
                            await _endCommonLogic.End(elementId, instance);
                            await RunAppDefinedOnTaskEnd(elementId, instance);
                            await _lockingCommonLogic.LockConnectedDataTypes(elementId, instance);
                            //These two services are scheduled to be removed and replaced by services tasks defined in the processfile.
                            await _pdfServiceTask.Execute(elementId, instance);
                            await _eformidlingServiceTask.Execute(elementId, instance);
                            break;
                        case InstanceEventType.process_AbandonTask:
                            await processTaskType.HandleTaskAbandon(elementId, instance);
                            await RunAppDefinedOnTaskAbandon(elementId, instance);
                            break;
                        case InstanceEventType.process_EndEvent:
                            await _appEvents.OnEndAppEvent(instanceEvent.ProcessInfo?.EndEvent, instance);
                            await RunAutoDeleteOnProcessEnd(instance);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Identify the correct task implementation
    /// </summary>
    /// <returns></returns>
    private IProcessTaskType GetProcessTask(string? altinnTaskType)
    {
        altinnTaskType ??= "NullType";
        foreach (var processTaskType in _processTaskTypes)
        {
            if (processTaskType.Key == altinnTaskType)
            {
                return processTaskType;
            }
        }

        throw new ArgumentException($"No process task type found for {altinnTaskType}");
    }

    private async Task RunAppDefinedOnTaskStart(string taskId, Instance instance,
        Dictionary<string, string> prefill)
    {
        foreach (var taskStart in _taskStarts)
        {
            await taskStart.Start(taskId, instance, prefill);
        }
    }

    private async Task RunAppDefinedOnTaskEnd(string endEvent, Instance instance)
    {
        foreach (var taskEnd in _taskEnds)
        {
            await taskEnd.End(endEvent, instance);
        }
    }

    private async Task RunAppDefinedOnTaskAbandon(string taskId, Instance instance)
    {
        foreach (var taskAbandon in _taskAbandons)
        {
            await taskAbandon.Abandon(taskId, instance);
        }

        _logger.LogDebug("OnAbandonProcessTask for {instanceId}. Locking data elements connected to {taskId}",
            instance.Id, taskId);
        await Task.CompletedTask;
    }
    
    private async Task RunAutoDeleteOnProcessEnd(Instance instance)
    {
        var instanceIdentifier = new InstanceIdentifier(instance);
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        if (appMetadata.AutoDeleteOnProcessEnd && instance.Process?.Ended != null)
        {
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
            await _instanceClient.DeleteInstance(instanceOwnerPartyId, instanceIdentifier.InstanceGuid, true);
        }
    }
}