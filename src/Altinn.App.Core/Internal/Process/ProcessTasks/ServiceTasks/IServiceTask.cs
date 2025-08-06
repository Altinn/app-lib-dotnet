using Altinn.App.Core.Features;

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
    public Task<ServiceTaskResult> Execute(ServiceTaskContext context);
}

/// <summary>
/// This class represents the parameters for executing a service task.
/// </summary>
public sealed record ServiceTaskContext
{
    /// <summary>
    /// An instance data mutator that can be used to read and modify the instance data during the service task execution.
    /// </summary>
    /// <remarks>Changes are saved after Execute returns a successful result. Keep in mind that data elements from previous tasks are locked.</remarks>
    public required IInstanceDataMutator InstanceDataMutator { get; init; }

    /// <summary>
    /// Cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}

/// <summary>
/// This class represents the result of executing a service task.
/// </summary>
public abstract class ServiceTaskResult { }

/// <summary>
/// This class represents a successful result of executing a service task.
/// </summary>
public sealed class ServiceTaskSuccessResult : ServiceTaskResult;

/// <summary>
/// This class represents a failed result of executing a service task.
/// </summary>
public sealed class ServiceTaskFailedResult : ServiceTaskResult;
