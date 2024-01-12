using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for form filling steps. 
    /// </summary>
    public class DataProcessTask : IProcessTask
    {
        /// <inheritdoc/>
        public string Type => "data";

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
        public async Task Start(string taskId, Instance instance, Dictionary<string, string> prefill)
        {
            await Task.CompletedTask;
        }
    }
}