using System.Text.Json.Serialization;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement.Models;

internal sealed class DelegationRequest
{
    [JsonPropertyName("from")]
    internal Delegator? From { get; set; }

    [JsonPropertyName("to")]
    internal Delegatee? To { get; set; }

    [JsonPropertyName("resourceId")]
    internal required string ResourceId { get; set; }

    [JsonPropertyName("instanceId")]
    internal required string InstanceId { get; set; }

    [JsonPropertyName("rights")]
    internal List<RightRequest> Rights { get; set; } = [];
}

internal sealed class RightRequest
{
    [JsonPropertyName("resource")]
    internal List<Resource> Resource { get; set; } = [];

    [JsonPropertyName("action")]
    internal AltinnAction? Action { get; set; }

    [JsonPropertyName("taskId")]
    internal string? TaskId { get; set; }
}
