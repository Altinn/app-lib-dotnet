using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;

/// <summary>
/// Response body from a successful Altinn app callback.
/// </summary>
public sealed record AppCallbackResponse
{
    [JsonPropertyName("state")]
    public string? State { get; init; }
}
