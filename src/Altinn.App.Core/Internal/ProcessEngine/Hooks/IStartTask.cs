using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Hooks;

/// <summary>
/// Hook interface for custom start task logic.
/// </summary>
[ImplementableByApps]
public interface IStartTask
{
    /// <summary>
    /// Determines whether the hook should run for the given task ID.
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public bool ShouldRunForTask(string taskId);

    public Task ExecuteAsync(StartTaskParameters parameters);
}
