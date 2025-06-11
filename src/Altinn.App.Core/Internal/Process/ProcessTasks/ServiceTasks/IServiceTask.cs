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
    public Task Execute(string taskId, Instance instance, CancellationToken cancellationToken = default);
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
