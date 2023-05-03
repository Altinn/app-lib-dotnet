using System.Security.Claims;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.Base;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.V2;

/// <summary>
/// Default implementation of the <see cref="IProcessEngine"/>
/// </summary>
public class ProcessEngine : IProcessEngine
{
    private readonly IInstance _instanceService;
    private readonly IProcessReader _processReader;
    private readonly IProfile _profileService;
    private readonly IProcess _processClient;
    private readonly IAppEvents _appEvents;
    private readonly ITaskEvents _taskEvents;
    private readonly IProcessNavigator _processNavigator;
    private readonly IEvents _eventsService;
    private readonly ILogger<ProcessEngine> _logger;
    private readonly bool _registerWithEventSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngine"/> class
    /// </summary>
    /// <param name="instanceService"></param>
    /// <param name="processReader"></param>
    /// <param name="profileService"></param>
    /// <param name="processClient"></param>
    /// <param name="appEvents"></param>
    /// <param name="taskEvents"></param>
    /// <param name="processNavigator"></param>
    /// <param name="eventsService"></param>
    /// <param name="appSettings"></param>
    /// <param name="logger"></param>
    public ProcessEngine(
        IInstance instanceService, 
        IProcessReader processReader,
        IProfile profileService, 
        IProcess processClient, 
        IAppEvents appEvents, 
        ITaskEvents taskEvents, 
        IProcessNavigator processNavigator, 
        IEvents eventsService, 
        IOptions<AppSettings> appSettings, 
        ILogger<ProcessEngine> logger)
    {
        _instanceService = instanceService;
        _processReader = processReader;
        _profileService = profileService;
        _processClient = processClient;
        _appEvents = appEvents;
        _taskEvents = taskEvents;
        _processNavigator = processNavigator;
        _eventsService = eventsService;
        _registerWithEventSystem = appSettings.Value.RegisterEventsWithEventsComponent;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ProcessChangeResult> StartProcess(ProcessStartRequest processStartRequest)
    {
        if (processStartRequest.Instance.Process != null)
        {
            return new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = "Process is already started. Use next.",
                ErrorType = "Conflict"
            };
        }

        string? validStartElement = ProcessHelper.GetValidStartEventOrError(processStartRequest.StartEventId, _processReader.GetStartEventIds(), out ProcessError? startEventError);
        if (startEventError != null)
        {
            return new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = "No matching startevent",
                ErrorType = "Conflict"
            };
        }

        // start process
        ProcessStateChange? startChange = await ProcessStart(processStartRequest.Instance, validStartElement!, processStartRequest.User);
        InstanceEvent? startEvent = startChange?.Events.First().CopyValues();
        ProcessStateChange nextChange = await ProcessNext(processStartRequest.Instance, processStartRequest.User);
        //ProcessChangeResult nextChange = await Next(processStartRequest.InstanceIdentifier, processStartRequest.User);
        InstanceEvent goToNextEvent = nextChange.Events.First().CopyValues();

        ProcessStateChange processStateChange = new ProcessStateChange
        {
            OldProcessState = startChange.OldProcessState,
            NewProcessState = nextChange.NewProcessState,
            Events = new List<InstanceEvent> { startEvent, goToNextEvent }
        };

        if (!processStartRequest.Dryrun)
        {
            await UpdateProcessAndDispatchEvents(processStartRequest.Instance, processStartRequest.Prefill, new List<InstanceEvent> { startEvent, goToNextEvent });
        }

        return new ProcessChangeResult()
        {
            Success = true,
            ProcessStateChange = processStateChange
        };
    }
     
    /// <inheritdoc/>
    public async Task<ProcessChangeResult> Next(ProcessNextRequest request)
    {
        var instance = request.Instance;
        string? currentElementId = instance.Process.CurrentTask?.ElementId;

        if (currentElementId == null)
        {
            return new ProcessChangeResult()
            {
                Success = false,
                ErrorMessage = $"Instance does not have current task information!",
                ErrorType = "Conflict"
            };
        }

        // Find next valid element. Later this will be dynamic
        ProcessElement nextElement = await _processNavigator.GetNextTask(instance, currentElementId, request.Action);

        var nextResult = await HandleMoveToNext(instance, request.User, request.Action);

        return new ProcessChangeResult()
        {
            Success = true,
            ProcessStateChange = nextResult
        };
    }

    /// <inheritdoc/>
    public async Task<Instance> UpdateInstanceAndRerunEvents(ProcessStartRequest startRequest, List<InstanceEvent> events)
    {
        return await UpdateProcessAndDispatchEvents(startRequest.Instance, startRequest.Prefill, events);
    }

    /// <summary>
    /// Does not save process. Instance object is updated.
    /// </summary>
    private async Task<ProcessStateChange?> ProcessStart(Instance instance, string startEvent, ClaimsPrincipal user)
    {
        if (instance.Process == null)
        {
            DateTime now = DateTime.UtcNow;

            ProcessState startState = new ProcessState
            {
                Started = now,
                StartEvent = startEvent,
                CurrentTask = new ProcessElementInfo { Flow = 1, ElementId = startEvent}
            };

            instance.Process = startState;

            List<InstanceEvent> events = new List<InstanceEvent>
            {
                await GenerateProcessChangeEvent(InstanceEventType.process_StartEvent.ToString(), instance, now, user),
            };

            return new ProcessStateChange
            {
                OldProcessState = null!,
                NewProcessState = startState,
                Events = events,
            };
        }

        return null;
    }

    /// <summary>
    /// Moves instance's process to nextElement id. Returns the instance together with process events.
    /// </summary>
    private async Task<ProcessStateChange> ProcessNext(Instance instance, ClaimsPrincipal userContext, string? action = null)
    {
        if (instance.Process != null)
        {
            ProcessStateChange result = new ProcessStateChange
            {
                OldProcessState = new ProcessState()
                {
                    Started = instance.Process.Started,
                    CurrentTask = instance.Process.CurrentTask,
                    StartEvent = instance.Process.StartEvent
                }
            };

            result.Events = await MoveProcessToNext(instance, userContext, action);
            result.NewProcessState = instance.Process;
            return result;
        }

        return null;
    }

    /// <summary>
    /// Assumes that nextElementId is a valid task/state
    /// </summary>
    private async Task<List<InstanceEvent>> MoveProcessToNext(
        Instance instance,
        ClaimsPrincipal user,
        string? action = null)
    {
        List<InstanceEvent> events = new List<InstanceEvent>();

        ProcessState previousState = instance.Process.Copy();
        ProcessState currentState = instance.Process;
        string? previousElementId = currentState.CurrentTask?.ElementId;

        ProcessElement nextElement = await _processNavigator.GetNextTask(instance, instance.Process.CurrentTask.ElementId, action);
        DateTime now = DateTime.UtcNow;
        bool previousIsProcessTask = _processReader.IsProcessTask(previousElementId);
        // ending previous element if task
        if (previousIsProcessTask)
        {
            instance.Process = previousState;
            events.Add(await GenerateProcessChangeEvent(InstanceEventType.process_EndTask.ToString(), instance, now, user));
            instance.Process = currentState;
        }

        // ending process if next element is end event
        if (_processReader.IsEndEvent(nextElement.Id))
        {
            currentState.CurrentTask = null;
            currentState.Ended = now;
            currentState.EndEvent = nextElement.Id;

            events.Add(await GenerateProcessChangeEvent(InstanceEventType.process_EndEvent.ToString(), instance, now, user));

            // add submit event (to support Altinn2 SBL)
            events.Add(await GenerateProcessChangeEvent(InstanceEventType.Submited.ToString(), instance, now, user));
        }
        else if (_processReader.IsProcessTask(nextElement.Id))
        {
            var task = nextElement as ProcessTask;
            currentState.CurrentTask = new ProcessElementInfo
            {
                Flow = currentState.CurrentTask?.Flow + 1,
                ElementId = nextElement.Id,
                Name = nextElement?.Name,
                Started = now,
                AltinnTaskType = task?.ExtensionElements?.AltinnProperties.TaskType,
                Validated = null,
            };

            events.Add(await GenerateProcessChangeEvent(InstanceEventType.process_StartTask.ToString(), instance, now, user));
        }

        // current state points to the instance's process object. The following statement is unnecessary, but clarifies logic.
        instance.Process = currentState;

        return events;
    }

    /// <summary>
    /// This 
    /// </summary>
    private async Task<Instance> UpdateProcessAndDispatchEvents(Instance instance, Dictionary<string, string>? prefill, List<InstanceEvent> events)
    {
        await HandleProcessChanges(instance, events, prefill);

        // need to update the instance process and then the instance in case appbase has changed it, e.g. endEvent sets status.archived
        Instance updatedInstance = await _instanceService.UpdateProcess(instance);
        await _processClient.DispatchProcessEventsToStorage(updatedInstance, events);

        // remember to get the instance anew since AppBase can have updated a data element or stored something in the database.
        updatedInstance = await _instanceService.GetInstance(updatedInstance);

        return updatedInstance;
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

    private async Task<InstanceEvent> GenerateProcessChangeEvent(string eventType, Instance instance, DateTime now, ClaimsPrincipal user)
    {
        int? userId = user.GetUserIdAsInt();
        InstanceEvent instanceEvent = new InstanceEvent
        {
            InstanceId = instance.Id,
            InstanceOwnerPartyId = instance.InstanceOwner.PartyId,
            EventType = eventType,
            Created = now,
            User = new PlatformUser
            {
                UserId = userId,
                AuthenticationLevel = user.GetAuthenticationLevel(),
                OrgId = user.GetOrg()
            },
            ProcessInfo = instance.Process,
        };

        if (string.IsNullOrEmpty(instanceEvent.User.OrgId) && userId != null)
        {
            UserProfile up = await _profileService.GetUserProfile((int)userId);
            instanceEvent.User.NationalIdentityNumber = up.Party.SSN;
        }

        return instanceEvent;
    }
    
    private async Task<ProcessStateChange?> HandleMoveToNext(Instance instance, ClaimsPrincipal user, string? action)
    {
        var processStateChange = await ProcessNext(instance, user, action);
        if (processStateChange != null)
        {
            instance = await UpdateProcessAndDispatchEvents(instance, new Dictionary<string, string>(), processStateChange.Events);

            await RegisterEventWithEventsComponent(instance);
        }

        return processStateChange;
    }
    
    private async Task RegisterEventWithEventsComponent(Instance instance)
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
}
