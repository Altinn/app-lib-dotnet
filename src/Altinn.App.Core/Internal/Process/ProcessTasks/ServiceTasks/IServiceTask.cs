using Altinn.App.Core.Features;
using Altinn.App.Core.Models.Process;

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
    public Task<ServiceTaskResult> Execute(ServiceTaskParameters parameters);
}

/// <summary>
/// This class represents the parameters for executing a service task.
/// </summary>
public sealed record ServiceTaskParameters
{
    /// <summary>
    /// An instance data mutator that can be used to read and modify the instance data during the service task execution.
    /// </summary>
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
public sealed class ServiceTaskSuccessResult : ServiceTaskResult { }

/// <summary>
/// This class represents a failed result of executing a service task.
/// </summary>
public sealed class ServiceTaskFailedResult : ServiceTaskResult
{
    /// <summary>
    /// Gets or sets the error title if the service task execution failed.
    /// </summary>
    public required string ErrorTitle { get; init; }

    /// <summary>
    /// Gets or sets the error message if the service task execution failed.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the error type if the service task execution failed.
    /// </summary>
    public required ProcessErrorType ErrorType { get; init; }

    /// <summary>
    /// Converts the service task failed result to an unsuccessful process change result.
    /// </summary>
    public ProcessChangeResult ToProcessChangeResult()
    {
        return new ProcessChangeResult
        {
            Success = false,
            ErrorTitle = ErrorTitle,
            ErrorMessage = ErrorMessage,
            ErrorType = ErrorType,
        };
    }
}
