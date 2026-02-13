using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Response model for workflow engine status endpoint.
/// </summary>
public sealed record WorkflowStatusResponse
{
    /// <summary>
    /// The overall status of the workflow for this instance.
    /// </summary>
    [JsonPropertyName("overallStatus")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PersistentItemStatus OverallStatus { get; init; }

    /// <summary>
    /// Details about each step in the workflow.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<StepDetail> Steps { get; init; }
}

/// <summary>
/// Details about a workflow engine step.
/// </summary>
public sealed record StepDetail
{
    /// <summary>
    /// The step identifier.
    /// </summary>
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    /// <summary>
    /// The command type.
    /// </summary>
    [JsonPropertyName("commandType")]
    public required string CommandType { get; init; }

    /// <summary>
    /// The current execution status.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PersistentItemStatus Status { get; init; }

    /// <summary>
    /// The number of times this step has been retried.
    /// </summary>
    [JsonPropertyName("retryCount")]
    public required int RetryCount { get; init; }

    /// <summary>
    /// When the step will next be eligible for execution (if backed off).
    /// </summary>
    [JsonPropertyName("backoffUntil")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? BackoffUntil { get; init; }
}
