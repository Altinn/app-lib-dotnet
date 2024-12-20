using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

/// <summary>
/// Represents the delegator.
/// </summary>
public class Delegator
{
    /// <summary>
    /// Gets or sets the type of the id.
    /// </summary>
    [JsonPropertyName("type")]
    public required string IdType { get; set; }

    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Id { get; set; }
}
