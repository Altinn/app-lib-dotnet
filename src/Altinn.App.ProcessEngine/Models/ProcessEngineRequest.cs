namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// A request to enqueue one or more task in the process engine.
/// </summary>
/// <param name="JobIdentifier"></param>
/// <param name="InstanceInformation"></param>
/// <param name="ProcessEngineActor"></param>
/// <param name="Tasks"></param>
public record ProcessEngineRequest(
    string JobIdentifier,
    InstanceInformation InstanceInformation,
    ProcessEngineActor ProcessEngineActor,
    IEnumerable<ProcessEngineCommandRequest> Tasks
)
{
    /// <summary>
    /// Determines whether the request is valid.
    /// </summary>
    public bool IsValid() => Tasks.Any();
};
