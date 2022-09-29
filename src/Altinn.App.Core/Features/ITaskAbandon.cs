using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
/// ITaskAbandon defines a implementation for running logic on task abandon
/// </summary>
public interface ITaskAbandon
{
    /// <summary>
    /// Method for defining custom process on TaskAbandoned event.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public Task ProcessEvent(string taskId, Instance instance);
}