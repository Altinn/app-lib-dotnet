using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

internal sealed class From
{
    [JsonPropertyName("type")]
    internal required string Type { get; set; }

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}
