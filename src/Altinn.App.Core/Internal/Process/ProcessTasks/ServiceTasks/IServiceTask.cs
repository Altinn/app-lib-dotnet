using Altinn.App.Core.Features;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

/// <summary>
/// Interface for service tasks that can be executed during a process.
/// </summary>
[ImplementableByApps]
public interface IServiceTask : IProcessTask
{
    /// <summary>
    /// Executes the service task.
    /// </summary>
    /// TODO: Fortsette å ta in taskId og instance, som de andre metodene, eller hoppe over på IInstanceDataAccessor?
    public Task Execute(string taskId, Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Method that is called to determine if the process should move to the next task after executing the service task, or wait for another process next call.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> MoveToNextTaskAfterExecution(
        string taskId,
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        // The default implementation is to move to the next task after execution
        return Task.FromResult(true);
    }
}

/// <summary>
/// This class represents the result of executing a service task.
/// </summary>
public class ServiceTaskResult
{
    /// <summary>
    /// The result of the service task execution.
    /// </summary>
    public ResultType Result { get; set; }

    /// <summary>
    /// Error type to return when the service task was not successful
    /// </summary>
    public ProcessErrorType? ErrorType { get; set; }

    /// <summary>
    /// Error message to return when the service task was not successful
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// An enum representing the status of the service task execution.
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        /// The service task was executed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The service task failed to execute.
        /// </summary>
        Failure,
    }
}
