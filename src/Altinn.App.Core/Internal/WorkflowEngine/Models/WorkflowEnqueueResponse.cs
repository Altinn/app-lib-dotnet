using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Response from the workflow engine enqueue endpoint.
/// </summary>
internal abstract record WorkflowEnqueueResponse
{
    private WorkflowEnqueueResponse() { }

    /// <summary>
    /// The request was accepted and workflows have been enqueued.
    /// </summary>
    internal sealed record Accepted : WorkflowEnqueueResponse
    {
        /// <summary>
        /// The enqueued workflow results.
        /// </summary>
        [JsonPropertyName("workflows")]
        public required IReadOnlyList<WorkflowResult> Workflows { get; init; }
    }

    /// <summary>
    /// The request was rejected by the engine.
    /// </summary>
    internal sealed record Rejected : WorkflowEnqueueResponse
    {
        /// <summary>
        /// The reason for rejection.
        /// </summary>
        [JsonPropertyName("reason")]
        public required Rejection Reason { get; init; }

        /// <summary>
        /// An optional human-readable message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}

/// <summary>
/// Result for an individual enqueued workflow.
/// </summary>
internal sealed record WorkflowResult
{
    /// <summary>
    /// The workflow reference, if provided in the request.
    /// </summary>
    [JsonPropertyName("ref")]
    public string? Ref { get; init; }

    /// <summary>
    /// The database ID assigned by the engine.
    /// </summary>
    [JsonPropertyName("databaseId")]
    public required long DatabaseId { get; init; }
}

/// <summary>
/// Reasons why a workflow enqueue request may be rejected.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum Rejection
{
    /// <summary>The request was invalid.</summary>
    Invalid,

    /// <summary>A duplicate workflow already exists.</summary>
    Duplicate,

    /// <summary>The engine is unavailable.</summary>
    Unavailable,

    /// <summary>The engine is at capacity.</summary>
    AtCapacity,

    /// <summary>A concurrency violation occurred.</summary>
    ConcurrencyViolation,
}
