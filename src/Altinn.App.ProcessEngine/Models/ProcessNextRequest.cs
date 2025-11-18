using System.Text.Json.Serialization;

namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// A request to move the process forward from one element (task) to another.
/// </summary>
public sealed record ProcessNextRequest
{
    /// <summary>
    /// The current BPMN element (task) ID.
    /// </summary>
    [JsonPropertyName("currentElementId")]
    public required string CurrentElementId { get; init; }

    /// <summary>
    /// The desired BPMN element (task) ID.
    /// </summary>
    [JsonPropertyName("desiredElementId")]
    public required string DesiredElementId { get; init; }

    /// <summary>
    /// Details about the instance this request belongs to.
    /// </summary>
    [JsonPropertyName("instanceInformation")]
    public required InstanceInformation InstanceInformation { get; init; }

    /// <summary>
    /// The actor this request is executed on behalf of.
    /// </summary>
    [JsonPropertyName("actor")]
    public required ProcessEngineActor Actor { get; init; }

    /// <summary>
    /// Process engine tasks associated with this request.
    /// </summary>
    [JsonPropertyName("tasks")]
    public required IEnumerable<ProcessEngineCommandRequest> Tasks { get; init; }

    /// <summary>
    /// Converts this request to a <see cref="ProcessEngineRequest"/> with a descriptive job identifier.
    /// </summary>
    internal ProcessEngineRequest ToProcessEngineRequest() =>
        new(
            $"{InstanceInformation.InstanceGuid}/next/from-{CurrentElementId}-to-{DesiredElementId}",
            InstanceInformation,
            Actor,
            Tasks
        );
};
