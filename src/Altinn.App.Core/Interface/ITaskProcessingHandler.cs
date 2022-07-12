using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Interface;

/// <summary>
/// ITaskProcessingHandler defines the methods that must be implemented by a task processing handler.
/// </summary>
public interface ITaskProcessingHandler
{
    /// <summary>
    /// Method for defining custom processing on TaskEnded event.
    /// </summary>
    /// <param name="taskId">The taskId</param>
    /// <param name="instance">The instance</param>
    public Task ProcessTaskEnd(string taskId, Instance instance);
}