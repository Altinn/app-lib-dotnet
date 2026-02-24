using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Response returned by the callback controller on success.
/// Contains the updated opaque state and an optional command-specific payload.
/// </summary>
public sealed record AppCallbackResponse
{
    /// <summary>
    /// Optional command-specific payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; init; }

    /// <summary>
    /// The updated opaque state to echo back to the engine.
    /// </summary>
    [JsonPropertyName("state")]
    public required JsonElement State { get; init; }
}
