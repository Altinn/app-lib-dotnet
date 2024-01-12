using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Implement this interface to create a new type of task for the process engine.
    /// </summary>
    public interface IProcessTask
    {
        /// <summary>
        /// The type is used to identify the correct task implementation for a given task type in the process config file.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Any logic to be executed when a task is started should be put in this method.
        /// </summary>
        Task Start(string elementId, Instance instance, Dictionary<string, string> prefill);

        /// <summary>
        /// Any logic to be executed when a task is ended should be put in this method.
        /// </summary>
        Task End(string elementId, Instance instance);

        /// <summary>
        /// Any logic to be executed when a task is abandoned should be put in this method.
        /// </summary>
        Task Abandon(string elementId, Instance instance);
    }
}
