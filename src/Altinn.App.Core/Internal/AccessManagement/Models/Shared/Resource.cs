using System.Text.Json.Serialization;
namespace Altinn.App.Core.Internal.AccessManagement.Models.Shared;

internal sealed class Resource
{
    [JsonPropertyName("type")]
    internal string Type { get; set; } = DelegationConst.Resource;

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}
