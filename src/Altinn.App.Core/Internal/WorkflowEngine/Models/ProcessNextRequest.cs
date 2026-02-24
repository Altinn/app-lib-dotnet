using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

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
    /// The actor this request is executed on behalf of.
    /// </summary>
    [JsonPropertyName("actor")]
    public required Actor Actor { get; init; }

    /// <summary>
    /// The lock token associated with this process/next request
    /// </summary>
    [JsonPropertyName("lockToken")]
    public required string LockToken { get; init; }

    /// <summary>
    /// Workflow steps associated with this request.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IEnumerable<StepRequest> Steps { get; init; }

    /// <summary>
    /// Opaque state blob to be echoed back on each callback.
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }
};
