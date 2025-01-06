using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.AltinnCdn;

internal sealed record AltinnCdnOrgs
{
    [JsonPropertyName("orgs")]
    public IReadOnlyDictionary<string, AltinnCdnOrgDetails>? Orgs { get; init; }
}

internal sealed record AltinnCdnOrgDetails
{
    [JsonPropertyName("name")]
    public AltinnCdnOrgName? Name { get; init; }

    [JsonPropertyName("logo")]
    public string? Logo { get; init; }

    [JsonPropertyName("orgnr")]
    public string? Orgnr { get; init; }

    [JsonPropertyName("homepage")]
    public string? Homepage { get; init; }

    [JsonPropertyName("environments")]
    public List<string>? Environments { get; init; }
}

internal sealed record AltinnCdnOrgName
{
    [JsonPropertyName("nb")]
    public string? Nb { get; init; }

    [JsonPropertyName("nn")]
    public string? Nn { get; init; }

    [JsonPropertyName("en")]
    public string? En { get; init; }
}
