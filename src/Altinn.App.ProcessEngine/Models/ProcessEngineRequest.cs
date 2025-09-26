using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// A request to enqueue one or more task in the process engine.
/// </summary>
public sealed record ProcessEngineRequest(
    AppIdentifier AppIdentifier,
    Instance Instance,
    IEnumerable<ProcessEngineTaskRequest> Tasks
)
{
    // TODO: Implement some basic validation here
    public bool IsValid() => Tasks.Any();
};
