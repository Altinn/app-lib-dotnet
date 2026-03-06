using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Represents a single workflow to be executed by the workflow engine.
/// </summary>
internal sealed record WorkflowRequest
{
    /// <summary>
    /// An optional reference for this workflow.
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; init; }

    /// <summary>
    /// A human-readable operation identifier (e.g. "Process next: Task_1 -> Task_2").
    /// </summary>
    [JsonPropertyName("operationId")]
    public required string OperationId { get; init; }

    /// <summary>
    /// A unique key for idempotency.
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public required string IdempotencyKey { get; init; }

    /// <summary>
    /// The type of workflow.
    /// </summary>
    [JsonPropertyName("type")]
    public required WorkflowType Type { get; init; }

    /// <summary>
    /// The steps to execute in this workflow.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IEnumerable<StepRequest> Steps { get; init; }

    /// <summary>
    /// An optional start time for when the workflow should begin execution.
    /// </summary>
    [JsonPropertyName("startAt")]
    public DateTimeOffset? StartAt { get; init; }

    /// <summary>
    /// Optional metadata associated with this workflow.
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; init; }

    /// <summary>
    /// Opaque state blob to be echoed back on each callback.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Optional workflow database IDs that must complete before this workflow starts.
    /// </summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<Guid>? Dependencies { get; init; }
}
