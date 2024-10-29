using System;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.AccessManagement.Models;

internal sealed class DelegationRequest
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
    internal List<Right> Rights { get; set; } = [];
}

internal sealed class From
{
    [JsonPropertyName("type")]
    internal required string Type { get; set; }

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}

internal sealed class To
{
    [JsonPropertyName("type")]
    internal required string Type { get; set; }

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}

internal sealed class Right
{
    [JsonPropertyName("resource")]
    internal List<Resource> Resource { get; set; } = [];

    [JsonPropertyName("action")]
    internal Action? Action { get; set; }
}

internal sealed class Resource
{
    [JsonPropertyName("type")]
    internal required string Type { get; set; }

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}

internal sealed class Action
{
    [JsonPropertyName("type")]
    internal required string Type { get; set; }

    [JsonPropertyName("value")]
    internal required string Value { get; set; }
}
