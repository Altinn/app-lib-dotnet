using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.EventHandlers
{
    /// <summary>
    /// This event handler is responsible for handling the end event for a process task.
    /// </summary>
    public class EndTaskEventHandler(ProcessTaskDataLocker processTaskDataLocker, ProcessTaskFinalizer processTaskFinisher, PdfServiceTask pdfServiceTask,
    EformidlingServiceTask eformidlingServiceTask, IEnumerable<IProcessTaskEnd> processTaskEnds) : IEndTaskEventHandler
    {
        /// <summary>
        /// Execute the event handler logic.
        /// </summary>
        /// <param name="processTask"></param>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public async Task Execute(IProcessTask processTask, string taskId, Instance instance)
        {
            await processTask.End(taskId, instance);
            await processTaskFinisher.Finalize(taskId, instance);
            await RunAppDefinedProcessTaskEndHandlers(taskId, instance);
            await processTaskDataLocker.Lock(taskId, instance);

            //These two services are scheduled to be removed and replaced by services tasks defined in the processfile.
            await pdfServiceTask.Execute(taskId, instance);
            await eformidlingServiceTask.Execute(taskId, instance);
        }

        /// <summary>
        /// Runs IProcessTaskEnds defined in the app.
        /// </summary>
        /// <param name="endEvent"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        private async Task RunAppDefinedProcessTaskEndHandlers(string endEvent, Instance instance)
        {
            foreach (var taskEnd in processTaskEnds)
            {
                await taskEnd.End(endEvent, instance);
            }
        }
    }
}
