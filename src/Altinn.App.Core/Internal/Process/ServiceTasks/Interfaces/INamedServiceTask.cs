namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// <see cref="IServiceTask"/> implementations that can be identified by a string identifier.
/// </summary>
public interface INamedServiceTask : IServiceTask
{
    /// <summary>
    /// The unique identifier of this service task.
    /// </summary>
    string Id { get; }
}
