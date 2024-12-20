using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

/// <summary>
/// Represents a resource.
/// </summary>
public class Resource
{
    /// <summary>
    /// Gets or sets the type of the resource. Default is "resource".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = DelegationConst.Resource;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
