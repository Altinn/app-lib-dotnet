using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// Response returned by the callback controller on success.
/// Contains the updated opaque state.
/// </summary>
public sealed record AppCallbackResponse
{
    /// <summary>
    /// The updated opaque state to echo back to the engine.
    /// </summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }
}
