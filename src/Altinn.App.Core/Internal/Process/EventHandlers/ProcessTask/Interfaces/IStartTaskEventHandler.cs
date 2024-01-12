using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.EventHandlers.ProcessTask
{
    /// <summary>
    /// Interface for start task event handlers, which are executed when a process start task event is triggered.
    /// </summary>
    public interface IStartTaskEventHandler
    {
        /// <summary>
        /// Execute the start task event handler
        /// </summary>
        /// <param name="processTask"></param>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <param name="prefill"></param>
        /// <returns></returns>
        Task Execute(IProcessTask processTask, string taskId, Instance instance, Dictionary<string, string> prefill);
    }
}