using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.TaskTypes
{
    /// <summary>
    /// Represents the process task responsible for form filling steps. 
    /// </summary>
    public class DataProcessTaskType : IProcessTaskType
    {
        /// <inheritdoc/>
        public string Key => "data";

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
        public async Task HandleTaskStart(string taskId, Instance instance, Dictionary<string, string> prefill)
        {
            await Task.CompletedTask;
        }
    }
}