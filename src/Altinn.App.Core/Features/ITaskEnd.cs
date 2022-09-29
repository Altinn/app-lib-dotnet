using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
/// ITaskEnd defines a implementation for running logic on task end
/// </summary>
public interface ITaskEnd
{
    /// <summary>
    /// Method for defining custom processing on TaskEnded event.
    /// </summary>
    /// <param name="taskId">The taskId</param>
    /// <param name="instance">The instance</param>
    public Task ProcessEvent(string taskId, Instance instance);   
}