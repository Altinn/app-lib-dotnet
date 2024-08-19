using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// Represents a service task that can be executed during a process. Replaces <see cref="IServiceTask"/>.
/// </summary>
public abstract class ServiceTaskBase : IProcessTask
{
    /// <inheritdoc />
    public abstract string Type { get; }

    /// <summary>
    /// Any logic to be executed when a task is started should be put in this method. The <see cref="Execute"/> method will be called from the base implementation of this method.
    /// </summary>
    public async Task Start(string taskId, Instance instance)
    {
        await Execute(taskId, instance);
    }

    /// <summary>
    /// Executes the main logic of the service task.
    /// </summary>
    protected abstract Task Execute(string taskId, Instance instance);

    /// <inheritdoc />
    public Task End(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Abandon(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }
}
