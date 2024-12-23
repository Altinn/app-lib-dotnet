using System.Text.Json.Serialization;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;

namespace Altinn.App.Core.Internal.AccessManagement.Models;

public sealed class DelegationResponse
{
    [JsonPropertyName("from")]
    public Delegator? Delegator { get; set; }

    [JsonPropertyName("to")]
    public Delegatee? Delegatee { get; set; }

    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("rights")]
    public List<RightResponse> Rights { get; set; } = [];
}

public sealed class RightResponse
{
    [JsonPropertyName("resource")]
    public List<Resource> Resource { get; set; } = [];

    [JsonPropertyName("action")]
    public AltinnAction? Action { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
