using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

internal sealed class Delegatee
{
    [JsonPropertyName("type")]
    internal required string IdType { get; set; }

    [JsonPropertyName("value")]
    internal required string Id { get; set; }
}
