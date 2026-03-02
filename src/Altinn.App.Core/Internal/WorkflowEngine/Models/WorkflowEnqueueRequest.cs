using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// A request to enqueue one or more workflows for execution.
/// </summary>
internal sealed record WorkflowEnqueueRequest
{
    /// <summary>
    /// The actor this request is executed on behalf of.
    /// </summary>
    [JsonPropertyName("actor")]
    public required Actor Actor { get; init; }

    /// <summary>
    /// The lock token associated with this request.
    /// </summary>
    [JsonPropertyName("lockToken")]
    public string? LockToken { get; init; }

    /// <summary>
    /// The workflows to enqueue.
    /// </summary>
    [JsonPropertyName("workflows")]
    public required IReadOnlyList<WorkflowRequest> Workflows { get; init; }
}
