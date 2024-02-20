using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Contains common logic for ending a process task.
    /// </summary>
    public interface IProcessTaskFinalizer
    {
        /// <summary>
        /// Runs common finalization logic for process tasks for a given task ID and instance. This method removes data elements generated from the task, removes hidden data and shadow fields.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        Task Finalize(string taskId, Instance instance);

        /// <summary>
        /// Removes data elements generated from a task, if the data elements are tagged with the task ID.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="endEvent"></param>
        /// <returns></returns>
        Task RemoveDataElementsGeneratedFromTask(Instance instance, string endEvent);
        
        /// <summary>
        /// Remove hidden data from the instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instanceGuid"></param>
        /// <param name="connectedDataTypes"></param>
        /// <returns></returns>
        Task RemoveHiddenData(Instance instance, Guid instanceGuid, List<DataType>? connectedDataTypes);
        
        /// <summary>
        /// Remove shadow fields from the instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instanceGuid"></param>
        /// <param name="connectedDataTypes"></param>
        /// <returns></returns>
        Task RemoveShadowFields(Instance instance, Guid instanceGuid, List<DataType> connectedDataTypes);
    }
}