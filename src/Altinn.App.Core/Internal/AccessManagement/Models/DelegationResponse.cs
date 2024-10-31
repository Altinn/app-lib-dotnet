using System.Text.Json.Serialization;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement.Models;

internal sealed class DelegationResponse
{
    [JsonPropertyName("from")]
    internal From? From { get; set; }

    [JsonPropertyName("to")]
    internal To? To { get; set; }

    [JsonPropertyName("resourceId")]
    internal string? ResourceId { get; set; }

    [JsonPropertyName("instanceId")]
    internal string? InstanceId { get; set; }

    [JsonPropertyName("rights")]
    internal List<RightResponse> Rights { get; set; } = [];
}

internal sealed class RightResponse
{
    [JsonPropertyName("resource")]
    internal List<Resource> Resource { get; set; } = [];

    [JsonPropertyName("action")]
    internal Action? Action { get; set; }

    [JsonPropertyName("status")]
    internal string? Status { get; set; }
}
