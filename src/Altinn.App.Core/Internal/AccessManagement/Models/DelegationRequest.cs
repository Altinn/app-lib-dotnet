using System.Text.Json.Serialization;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement.Models;

internal sealed class DelegationRequest
{
    [JsonPropertyName("from")]
    public Delegator? From { get; set; }

    [JsonPropertyName("to")]
    public Delegatee? To { get; set; }

    [JsonPropertyName("resourceId")]
    public required string ResourceId { get; set; }

    [JsonPropertyName("instanceId")]
    public required string InstanceId { get; set; }

    [JsonPropertyName("rights")]
    public List<RightRequest> Rights { get; set; } = [];
}

internal sealed class RightRequest
{
    [JsonPropertyName("resource")]
    public List<Resource> Resource { get; set; } = [];

    [JsonPropertyName("action")]
    public AltinnAction? Action { get; set; }

    [JsonPropertyName("taskId")]
    public string? TaskId { get; set; }
}
