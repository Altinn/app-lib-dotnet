using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

/// <summary>
/// Represents an action.
/// </summary>
public class AltinnAction
{
    /// <summary>
    /// Gets or sets the type of the action.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
