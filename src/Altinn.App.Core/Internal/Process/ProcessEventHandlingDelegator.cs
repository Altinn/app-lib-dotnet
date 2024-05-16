using Altinn.App.Core.Internal.Process.EventHandlers;
using Altinn.App.Core.Internal.Process.EventHandlers.ProcessTask;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process
{
    /// <summary>
    /// This class is responsible for delegating process events to the correct event handler.
    /// </summary>
    public class ProcessEventHandlingDelegator : IProcessEventHandlerDelegator
    {
        private readonly ILogger<ProcessEventHandlingDelegator> _logger;
        private readonly IStartTaskEventHandler _startTaskEventHandler;
        private readonly IEndTaskEventHandler _endTaskEventHandler;
        private readonly IAbandonTaskEventHandler _abandonTaskEventHandler;
        private readonly IEndEventEventHandler _endEventHandler;
        private readonly IEnumerable<IProcessTask> _processTasks;

        /// <summary>
        /// This class is responsible for delegating process events to the correct event handler.
        /// </summary>
        public ProcessEventHandlingDelegator(
            ILogger<ProcessEventHandlingDelegator> logger,
            IStartTaskEventHandler startTaskEventHandler,
            IEndTaskEventHandler endTaskEventHandler,
            IAbandonTaskEventHandler abandonTaskEventHandler,
            IEndEventEventHandler endEventHandler,
            IEnumerable<IProcessTask> processTasks
        )
        {
            _logger = logger;
            _startTaskEventHandler = startTaskEventHandler;
            _endTaskEventHandler = endTaskEventHandler;
            _abandonTaskEventHandler = abandonTaskEventHandler;
            _endEventHandler = endEventHandler;
            _processTasks = processTasks;
        }

        /// <summary>
        /// Loops through all events and delegates the event to the correct event handler.
        /// </summary>
        public async Task HandleEvents(
            Instance instance,
            Dictionary<string, string>? prefill,
            List<InstanceEvent>? events
        )
        {
            if (events == null)
            {
                return;
            }

            foreach (InstanceEvent instanceEvent in events)
            {
                if (Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType eventType))
                {
                    if (instanceEvent.ProcessInfo?.CurrentTask != null)
                    {
                        await HandleTaskEvent(instance, eventType, instanceEvent.ProcessInfo.CurrentTask, prefill);
                    }
                    else
                    {
                        await HandleNonTaskEvent(instance, eventType, instanceEvent);
                    }
                }
                else
                {
                    _logger.LogError("Unable to parse instanceEvent eventType {EventType}", instanceEvent.EventType);
                }
            }
        }

        private async Task HandleTaskEvent(
            Instance instance,
            InstanceEventType eventType,
            ProcessElementInfo currentTask,
            Dictionary<string, string>? prefill
        )
        {
            string taskId = currentTask.ElementId;
            string altinnTaskType = currentTask.AltinnTaskType;

            switch (eventType)
            {
                case InstanceEventType.process_StartTask:
                    await _startTaskEventHandler.Execute(
                        GetProcessTaskInstance(altinnTaskType),
                        taskId,
                        instance,
                        prefill
                    );
                    break;
                case InstanceEventType.process_EndTask:
                    await _endTaskEventHandler.Execute(GetProcessTaskInstance(altinnTaskType), taskId, instance);
                    break;
                case InstanceEventType.process_AbandonTask:
                    // InstanceEventType is set to Abandon when action performed is `Reject`. This is to keep backwards compatability with existing code that only should be run when a task is abandoned/rejected.
                    await _abandonTaskEventHandler.Execute(GetProcessTaskInstance(altinnTaskType), taskId, instance);
                    break;

                default:
                    _logger.LogInformation(
                        "No handler found for task eventType {EventType}. Won't do anything.",
                        eventType
                    );
                    break;
            }
        }

        private async Task HandleNonTaskEvent(
            Instance instance,
            InstanceEventType eventType,
            InstanceEvent instanceEvent
        )
        {
            switch (eventType)
            {
                case InstanceEventType.process_StartEvent:
                    break;

                case InstanceEventType.process_EndEvent:
                    await _endEventHandler.Execute(instanceEvent, instance);
                    break;
                default:
                    _logger.LogInformation("No handler found for eventType {EventType}. Won't do anything.", eventType);
                    break;
            }
        }

        /// <summary>
        /// Identify the correct task implementation
        /// </summary>
        private IProcessTask GetProcessTaskInstance(string? altinnTaskType)
        {
            if (string.IsNullOrEmpty(altinnTaskType))
            {
                altinnTaskType = "NullType";
            }

            IProcessTask? processTask = _processTasks.FirstOrDefault(pt => pt.Type == altinnTaskType);

            if (processTask == null)
            {
                throw new ProcessException($"No process task instance found for altinnTaskType {altinnTaskType}");
            }

            return processTask;
        }
    }
}
