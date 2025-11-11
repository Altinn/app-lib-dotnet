namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// A request to enqueue one or more task in the process engine.
/// </summary>
public record ProcessEngineRequest(
    string JobIdentifier,
    InstanceInformation InstanceInformation,
    ProcessEngineActor ProcessEngineActor,
    IEnumerable<ProcessEngineCommandRequest> Tasks,
    DateTimeOffset? CreatedAt = null
)
{
    // TODO: Implement some basic validation here
    public bool IsValid() => Tasks.Any();
};
