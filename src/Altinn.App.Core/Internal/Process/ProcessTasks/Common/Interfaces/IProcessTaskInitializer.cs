using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Contains common logic for initializing a process task.
    /// </summary>
    public interface IProcessTaskInitializer
    {
        /// <summary>
        /// Runs common initialization logic for process tasks for a given task ID and instance. This method initializes the data elements for the instance based on application metadata and prefill configurations. Also updates presentation texts and data values on the instance.
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="instance"></param>
        /// <param name="prefill"></param>
        Task Initialize(string taskId, Instance instance, Dictionary<string, string> prefill);

        
        /// <summary>
        /// Updates presentation texts on the instance for a given data type and data.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="dataType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, dynamic data);
        
        /// <summary>
        /// Updates data values on the instance for a given data type and data.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="dataType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task UpdateDataValuesOnInstance(Instance instance, string dataType, object data);
    }
}