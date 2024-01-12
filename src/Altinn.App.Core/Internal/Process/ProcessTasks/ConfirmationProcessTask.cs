using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user confirmation.
    /// </summary>
    public class ConfirmationProcessTask : IProcessTask
    {
        /// <inheritdoc/>
        public string Type => "confirmation";

        /// <inheritdoc/>
        public async Task Abandon(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task End(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Start(string elementId, Instance instance, Dictionary<string, string> prefill)
        {
            await Task.CompletedTask;
        }
    }
}
