using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.TaskTypes
{
    /// <summary>
    /// Represents the process task responsible for waiting for feedback from application owner.
    /// </summary>
    public class FeedbackProcessTaskType : IProcessTaskType
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackProcessTaskType"/> class.
        /// </summary>
        public FeedbackProcessTaskType()
        {
        }
        
        /// <inheritdoc/>
        public string Key => "feedback";

        /// <inheritdoc/>
        public async Task HandleTaskAbandon(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task HandleTaskComplete(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task HandleTaskStart(string elementId, Instance instance, Dictionary<string, string> prefill)
        {
            await Task.CompletedTask;
        }
    }
}
