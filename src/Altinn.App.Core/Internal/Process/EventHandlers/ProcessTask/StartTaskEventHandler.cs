using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.EventHandlers
{
    /// <summary>
    /// This event handler is responsible for handling the start event for a process task.
    /// </summary>
    public class StartTaskEventHandler(ProcessTaskDataLocker processTaskDataLocker, ProcessTaskInitializer processTaskInitializer, IEnumerable<IProcessTaskStart> processTaskStarts) : IStartTaskEventHandler
    {
        /// <summary>
        /// Execute the event handler logic.
        /// </summary>
        /// <param name="processTask"></param>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <param name="prefill"></param>
        /// <returns></returns>
        public async Task Execute(IProcessTask processTask, string taskId, Instance instance, Dictionary<string, string> prefill)
        {
            await processTaskDataLocker.Unlock(taskId, instance);
            await RunAppDefinedProcessTaskStartHandlers(taskId, instance, prefill);
            await processTaskInitializer.Initialize(taskId, instance, prefill);
            await processTask.Start(taskId, instance, prefill);
        }

        /// <summary>
        /// Runs IProcessTaskStarts defined in the app.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <param name="prefill"></param>
        /// <returns></returns>
        private async Task RunAppDefinedProcessTaskStartHandlers(string taskId, Instance instance,
            Dictionary<string, string> prefill)
        {
            foreach (var processTaskStarts in processTaskStarts)
            {
                await processTaskStarts.Start(taskId, instance, prefill);
            }
        }
    }
}
