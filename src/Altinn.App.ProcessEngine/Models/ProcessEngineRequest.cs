namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// A request to enqueue one or more task in the process engine.
/// </summary>
/// <param name="JobIdentifier">The job identifier. A unique-ish keyword describing the job.</param>
/// <param name="InstanceInformation">Information about the instance this job relates to.</param>
/// <param name="Actor">The actor this request is executed on behalf of.</param>
/// <param name="Tasks">The individual tasks comprising this job.</param>
public record ProcessEngineRequest(
    string JobIdentifier,
    InstanceInformation InstanceInformation,
    ProcessEngineActor Actor,
    IEnumerable<ProcessEngineCommandRequest> Tasks
)
{
    /// <summary>
    /// Determines whether the request is valid.
    /// </summary>
    public bool IsValid() => Tasks.Any();
};
