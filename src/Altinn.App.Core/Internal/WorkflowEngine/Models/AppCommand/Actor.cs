using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models.AppCommand;

/// <summary>
/// Represents the user/entity on whose behalf the engine is executing tasks.
/// </summary>
public sealed record Actor
{
    [JsonPropertyName("userIdOrOrgNumber")]
    public required string UserIdOrOrgNumber { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }
}
