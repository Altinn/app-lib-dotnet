using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.TaskTypes
{
    /// <summary>
    /// Represents the process task responsible for collecting user confirmation.
    /// </summary>
    public class ConfirmationProcessTaskType : IProcessTaskType
    {
        /// <inheritdoc/>
        public string Key => "confirmation";

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
