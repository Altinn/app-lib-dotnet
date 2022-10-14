using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Helpers
{
    /// <summary>
    /// Helper class for handling the process for an instance.
    /// </summary>
    public class ProcessHelper
    {
        private readonly IProcessReader _processReader;
        
        /// <summary>
        /// Initialize a new instance of the <see cref="ProcessHelper"/> class with the given data stream.
        /// </summary>
        /// <param name="bpmnStream">A stream with access to a BPMN file.</param>
        public ProcessHelper(IProcessReader processReader)
        {
            _processReader = processReader;
        }

        /// <summary>
        /// Try to get the next valid step in the process.
        /// </summary>
        /// <param name="currentElement">The current element name.</param>
        /// <param name="nextElementError">Any error preventing the logic to identify next element.</param>
        /// <returns>The name of the next element.</returns>
        public string? GetValidNextElementOrError(string currentElement, out ProcessError? nextElementError)
        {
            nextElementError = null;
            string? nextElementId = null;

            List<string> nextElements = _processReader.GetNextElementIds(currentElement, true);

            if (nextElements.Count > 1)
            {
                nextElementError = new ProcessError
                {
                    Code = "Conflict",
                    Text = $"There is more than one element reachable from element {currentElement}"
                };
            }
            else
            {
                nextElementId = nextElements.First();
            }

            return nextElementId;
        }

        /// <summary>
        /// Checks whether the given element id is a task.
        /// </summary>
        /// <param name="nextElementId">The name of an element from the process.</param>
        /// <returns>True if the element is a task.</returns>
        public bool IsTask(string nextElementId)
        {
            List<string> tasks = _processReader.GetProcessTaskIds();
            return tasks.Contains(nextElementId);
        }

        /// <summary>
        /// Checks whether the given element id is a start event.
        /// </summary>
        /// <param name="startEventId">The name of an element from the process.</param>
        /// <returns>True if the element is a start event.</returns>
        public bool IsStartEvent(string startEventId)
        {
            List<string> startEvents = _processReader.GetStartEventIds();
            return startEvents.Contains(startEventId);
        }

        /// <summary>
        /// Checks whether the given element id is an end event.
        /// </summary>
        /// <param name="nextElementId">The name of an element from the process.</param>
        /// <returns>True if the element is an end event.</returns>
        public bool IsEndEvent(string nextElementId)
        {
            List<string> endEvents = _processReader.GetEndEventIds();
            return endEvents.Contains(nextElementId);
        }

        /// <summary>
        /// Validates that the process can start from the given start event.
        /// </summary>
        /// <param name="proposedStartEvent">The name of the start event the process should start from.</param>
        /// <param name="startEventError">Any error preventing the process from starting.</param>
        /// <returns>The name of the start event or null if start event wasn't found.</returns>
        public string? GetValidStartEventOrError(string proposedStartEvent, out ProcessError? startEventError)
        {
            startEventError = null;

            List<string> possibleStartEvents = _processReader.GetStartEventIds();

            if (!string.IsNullOrEmpty(proposedStartEvent))
            {
                if (possibleStartEvents.Contains(proposedStartEvent))
                {
                    return proposedStartEvent;
                }
                else
                {
                    startEventError = Conflict($"There is no such start event as '{proposedStartEvent}' in the process definition.");
                    return null;
                }
            }

            if (possibleStartEvents.Count == 1)
            {
                return possibleStartEvents.First();
            }
            else if (possibleStartEvents.Count > 1)
            {
                startEventError = Conflict($"There are more than one start events available. Chose one: {possibleStartEvents}");
                return null;
            }
            else
            {
                startEventError = Conflict($"There is no start events in process definition. Cannot start process!");
                return null;
            }
        }

        /// <summary>
        /// Validates that the given element name is a valid next step in the process.
        /// </summary>
        /// <param name="currentElementId">The current element name.</param>
        /// <param name="proposedElementId">The name of the proposed next element.</param>
        /// <param name="nextElementError">Any error preventing the logic to identify next element.</param>
        /// <returns>The name of the next element.</returns>
        public string? GetValidNextElementOrError(string currentElementId, string proposedElementId, out ProcessError? nextElementError)
        {
            nextElementError = null;
            bool useGatewayDefaults = string.IsNullOrEmpty(proposedElementId);

            List<string> possibleNextElements = _processReader.GetNextElementIds(currentElementId, true, useGatewayDefaults);

            if (!string.IsNullOrEmpty(proposedElementId))
            {
                if (possibleNextElements.Contains(proposedElementId))
                {
                    return proposedElementId;
                }
                else
                {
                    nextElementError = Conflict($"The proposed next element id '{proposedElementId}' is not among the available next process elements");
                    return null;
                }
            }

            if (possibleNextElements.Count == 1)
            {
                return possibleNextElements.First();
            }

            if (possibleNextElements.Count > 1)
            {
                nextElementError = Conflict($"There are more than one outgoing sequence flows, please select one '{possibleNextElements}'");
                return null;
            }

            if (possibleNextElements.Count == 0)
            {
                nextElementError = Conflict($"There are no outoging sequence flows from current element. Cannot find next process element. Error in bpmn file!");
                return null;
            }

            return null;
        }

        /// <summary>
        /// Find the flowtype betweeend 
        /// </summary>
        public ProcessSequenceFlowType GetSequenceFlowType(string currentId, string nextElementId)
        {
            List<SequenceFlow> flows = _processReader.GetSequenceFlowsBetween(currentId, nextElementId);
            foreach (SequenceFlow flow in flows)
            { 
                if (!string.IsNullOrEmpty(flow.FlowType))
                {
                    ProcessSequenceFlowType flowType;
                    if (Enum.TryParse(flow.FlowType, out flowType))
                    {
                        return flowType;
                    }
                }
            }

            return ProcessSequenceFlowType.CompleteCurrentMoveToNext;
        }
        
        /// <summary>
        ///  Called before a process task is ended. App can do extra validation logic and add validation issues to collection which will be returned by the controller.
        /// </summary>
        /// <param name="taskId">The id of the task to be ended.</param>
        /// <param name="instance">The instance to be ended.</param>
        /// <param name="validationIssues">The collection of validation issues.</param> 
        /// <returns>true task can be ended, false otherwise</returns>
        public static async Task<bool> CanEndProcessTask(string taskId, Instance instance,
            List<ValidationIssue> validationIssues)
        {
            // check if the task is validated
            if (instance.Process?.CurrentTask?.Validated != null)
            {
                ValidationStatus validationStatus = instance.Process.CurrentTask.Validated;

                if (validationStatus.CanCompleteTask)
                {
                    return true;
                }
            }
            else
            {
                if (validationIssues.Count == 0)
                {
                    return true;
                }
            }

            return await Task.FromResult(false);
        }

        private ProcessError Conflict(string text)
        {
            return new ProcessError
            {
                Code = "Conflict",
                Text = text
            };
        }
    }
}
