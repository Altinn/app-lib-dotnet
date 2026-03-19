using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;

/// <summary>
/// Response model for workflow engine status endpoint.
/// </summary>
internal sealed record WorkflowStatusResponse
{
    /// <summary>
    /// The database ID of the workflow.
    /// </summary>
    [JsonPropertyName("databaseId")]
    public Guid DatabaseId { get; init; }

    /// <summary>
    /// The correlation ID for this workflow, if one was provided.
    /// </summary>
    [JsonPropertyName("correlationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CorrelationId { get; init; }

    /// <summary>
    /// The operation ID of the workflow.
    /// </summary>
    [JsonPropertyName("operationId")]
    public required string OperationId { get; init; }

    /// <summary>
    /// The idempotency key of the workflow.
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public required string IdempotencyKey { get; init; }

    /// <summary>
    /// The namespace this workflow belongs to.
    /// </summary>
    [JsonPropertyName("namespace")]
    public required string Namespace { get; init; }

    /// <summary>
    /// When the workflow was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the workflow was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// When the workflow should start execution.
    /// </summary>
    [JsonPropertyName("startAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? StartAt { get; init; }

    /// <summary>
    /// When the workflow will next be eligible for execution.
    /// </summary>
    [JsonPropertyName("backoffUntil")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? BackoffUntil { get; init; }

    /// <summary>
    /// When cancellation was requested for this workflow, if applicable.
    /// </summary>
    [JsonPropertyName("cancellationRequestedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? CancellationRequestedAt { get; init; }

    /// <summary>
    /// Labels associated with this workflow.
    /// </summary>
    [JsonPropertyName("labels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// The overall status of the workflow.
    /// </summary>
    [JsonPropertyName("overallStatus")]
    public required PersistentItemStatus OverallStatus { get; init; }

    /// <summary>
    /// Optional dependency information.
    /// </summary>
    [JsonPropertyName("dependencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<Guid, PersistentItemStatus>? Dependencies { get; init; }

    /// <summary>
    /// Optional link information.
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<Guid, PersistentItemStatus>? Links { get; init; }

    /// <summary>
    /// Details about each step in the workflow.
    /// </summary>
    [JsonPropertyName("steps")]
    public required IReadOnlyList<StepStatusResponse> Steps { get; init; }
}

/// <summary>
/// Status details about a workflow engine step.
/// </summary>
internal sealed record StepStatusResponse
{
    /// <summary>
    /// The database ID of the step.
    /// </summary>
    [JsonPropertyName("databaseId")]
    public Guid DatabaseId { get; init; }

    /// <summary>
    /// The idempotency key of the step.
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// An identifier for this operation.
    /// </summary>
    [JsonPropertyName("operationId")]
    public required string OperationId { get; init; }

    /// <summary>
    /// The processing order of this step within the workflow.
    /// </summary>
    [JsonPropertyName("processingOrder")]
    public required int ProcessingOrder { get; init; }

    /// <summary>
    /// When the step was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Labels associated with the step.
    /// </summary>
    [JsonPropertyName("labels")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// The command details for this step.
    /// </summary>
    [JsonPropertyName("command")]
    public required CommandDetails Command { get; init; }

    /// <summary>
    /// The current execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public required PersistentItemStatus Status { get; init; }

    /// <summary>
    /// The number of times this step has been retried.
    /// </summary>
    [JsonPropertyName("retryCount")]
    public required int RetryCount { get; init; }

    /// <summary>
    /// The retry strategy for this step.
    /// </summary>
    [JsonPropertyName("retryStrategy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RetryStrategy? RetryStrategy { get; init; }

    /// <summary>
    /// Details about the command associated with a step.
    /// </summary>
    internal sealed record CommandDetails
    {
        /// <summary>
        /// The command type (e.g. "app", "webhook").
        /// </summary>
        [JsonPropertyName("type")]
        public required string Type { get; init; }
    }
}
