using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.EventHandlers
{
    /// <summary>
    /// This event handler is responsible for handling the abandon event for a process task.
    /// </summary>
    /// <param name="processTaskAbondons"></param>
    public class AbandonTaskEventHandler(IEnumerable<IProcessTaskAbandon> processTaskAbondons) : IAbandonTaskEventHandler
    {
        /// <summary>
        /// Handles the abandon event for a process task.
        /// </summary>
        /// <param name="processTask"></param>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public async Task Execute(IProcessTask processTask, string taskId, Instance instance)
        {
            await processTask.Abandon(taskId, instance);
            await RunAppDefinedProcessTaskAbandonHandlers(taskId, instance);
        }

        /// <summary>
        /// Runs IProcessTaskAbandons defined in the app.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        private async Task RunAppDefinedProcessTaskAbandonHandlers(string taskId, Instance instance)
        {
            foreach (var taskAbandon in processTaskAbondons)
            {
                await taskAbandon.Abandon(taskId, instance);
            }
        }
    }
}
