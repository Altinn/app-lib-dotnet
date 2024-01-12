using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for signing.
    /// </summary>
    internal class SigningProcessTask : IProcessTask
    {
        public string Type => "signing";

        /// <inheritdoc/>
        public async Task Start(string elementId, Instance instance, Dictionary<string, string> prefill)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task End(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Abandon(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }
    }
}
