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
        /// <param name="processReader">IProcessReader for reading process</param>
        public ProcessHelper(IProcessReader processReader)
        {
            _processReader = processReader;
        }

        /// <summary>
        /// Validates that the process can start from the given start event.
        /// </summary>
        /// <param name="proposedStartEvent">The name of the start event the process should start from.</param>
        /// <param name="possibleStartEvents">List of possible start events <see cref="IProcessReader.GetStartEventIds"/></param>
        /// <param name="startEventError">Any error preventing the process from starting.</param>
        /// <returns>The name of the start event or null if start event wasn't found.</returns>
        public static string? GetValidStartEventOrError(string? proposedStartEvent, List<string> possibleStartEvents, out ProcessError? startEventError)
        {
            startEventError = null;

            if (!string.IsNullOrEmpty(proposedStartEvent))
            {
                if (possibleStartEvents.Contains(proposedStartEvent))
                {
                    return proposedStartEvent;
                }

                startEventError = Conflict($"There is no such start event as '{proposedStartEvent}' in the process definition.");
                return null;
            }

            if (possibleStartEvents.Count == 1)
            {
                return possibleStartEvents.First();
            }

            if (possibleStartEvents.Count > 1)
            {
                startEventError = Conflict($"There are more than one start events available. Chose one: [{string.Join(", ", possibleStartEvents)}]");
                return null;
            }

            startEventError = Conflict($"There is no start events in process definition. Cannot start process!");
            return null;
        }

        /// <summary>
        /// Validates that the given element name is a valid next step in the process.
        /// </summary>
        /// <param name="proposedElementId">The name of the proposed next element.</param>
        /// <param name="possibleNextElements">List of possible next elements</param>
        /// <param name="nextElementError">Any error preventing the logic to identify next element.</param>
        /// <returns>The name of the next element.</returns>
        public static string? GetValidNextElementOrError(string? proposedElementId, List<string> possibleNextElements, out ProcessError? nextElementError)
        {
            nextElementError = null;

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
                nextElementError = Conflict($"There are no outgoing sequence flows from current element. Cannot find next process element. Error in bpmn file!");
                return null;
            }

            return null;
        }

        /// <summary>
        /// Find the flowtype between 
        /// </summary>
        public static ProcessSequenceFlowType GetSequenceFlowType(List<SequenceFlow> flows)
        {
            foreach (SequenceFlow flow in flows)
            {
                if (!string.IsNullOrEmpty(flow.FlowType))
                {
                    if (Enum.TryParse(flow.FlowType, out ProcessSequenceFlowType flowType))
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


        /// <summary>
        /// Checks whether the given element id is a task.
        /// </summary>
        /// <param name="nextElementId">The name of an element from the process.</param>
        /// <returns>True if the element is a task.</returns>
        [Obsolete("Method is deprecated and will be removed. Please use IProcessReader.IsTask(string) instead", false)]
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
        [Obsolete("Method is deprecated and will be removed. Please use IProcessReader.IsStartEvent(string) instead", false)]
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
        [Obsolete("Method is deprecated and will be removed. Please use IProcessReader.IsEndEvent(string) instead", false)]
        public bool IsEndEvent(string nextElementId)
        {
            List<string> endEvents = _processReader.GetEndEventIds();
            return endEvents.Contains(nextElementId);
        }

        private static ProcessError Conflict(string text)
        {
            return new ProcessError
            {
                Code = "Conflict",
                Text = text
            };
        }
    }
}
