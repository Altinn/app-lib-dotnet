#nullable disable

using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the request body for setting a set of tags on a data element.
/// </summary>
public class SetTagsRequest
{
    /// <summary>
    /// A list of tags to set on the data element represented as string values.
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}
