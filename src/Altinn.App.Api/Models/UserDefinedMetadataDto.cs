#nullable disable
using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the response from an API endpoint providing a list of key-value properties.
/// </summary>
public class UserDefinedMetadataDto
{
    /// <summary>
    /// A list of properties represented as key-value pairs.
    /// </summary>
    [JsonPropertyName("userDefinedMetadata")]
    public List<KeyValueEntry> UserDefinedMetadata { get; init; } = [];

    /// <summary>
    /// Validates that all keys in the CustomMetadata list are unique.
    /// </summary>
    public List<string> FindDuplicatedKeys()
    {
        return UserDefinedMetadata
            .GroupBy(entry => entry.Key)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
    }
}
