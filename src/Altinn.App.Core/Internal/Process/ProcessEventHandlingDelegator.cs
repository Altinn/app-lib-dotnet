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
        public ProcessEventHandlingDelegator(ILogger<ProcessEventHandlingDelegator> logger,
            IStartTaskEventHandler startTaskEventHandler,
            IEndTaskEventHandler endTaskEventHandler,
            IAbandonTaskEventHandler abandonTaskEventHandler,
            IEndEventEventHandler endEventHandler,
            IEnumerable<IProcessTask> processTasks)
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
        /// <param name="instance"></param>
        /// <param name="events"></param>
        /// <param name="prefill"></param>
        /// <returns></returns>
        public async Task HandleEvents(Instance instance, Dictionary<string, string>? prefill, List<InstanceEvent>? events)
        {
            if (events == null)
            {
                return;
            }

            foreach (InstanceEvent instanceEvent in events)
            {
                if (Enum.TryParse(instanceEvent.EventType, true, out InstanceEventType eventType))
                {
                    string? taskId = instanceEvent.ProcessInfo?.CurrentTask?.ElementId; //TODO: What do we do if taskId is null?
                    string? altinnTaskType = instanceEvent.ProcessInfo?.CurrentTask?.AltinnTaskType;

                    switch (eventType)
                    {
                        case InstanceEventType.process_StartEvent:
                            break;
                        case InstanceEventType.process_StartTask:
                            await _startTaskEventHandler.Execute(GetProcessTaskInstance(altinnTaskType), taskId, instance, prefill ?? []);
                            break;
                        case InstanceEventType.process_EndTask:
                            await _endTaskEventHandler.Execute(GetProcessTaskInstance(altinnTaskType), taskId, instance);
                            break;
                        case InstanceEventType.process_AbandonTask:
                            await _abandonTaskEventHandler.Execute(GetProcessTaskInstance(altinnTaskType), taskId, instance);
                            break;
                        case InstanceEventType.process_EndEvent:
                            await _endEventHandler.Execute(instanceEvent, instance);
                            break;
                    }
                }
                else
                {
                    _logger.LogError("Unable to parse instanceEvent eventType {EventType}", instanceEvent.EventType);
                }
            }
        }

        /// <summary>
        /// Identify the correct task implementation
        /// </summary>
        /// <returns></returns>
        private IProcessTask GetProcessTaskInstance(string? altinnTaskType)
        {
            altinnTaskType ??= "NullType";
            foreach (var processTaskType in _processTasks)
            {
                if (processTaskType.Type == altinnTaskType)
                {
                    return processTaskType;
                }
            }

            throw new ProcessException($"No process task instance found for altinnTaskType {altinnTaskType}");
        }
    }
}
