using System.Net.Http.Json;
using System.Text.Json;

namespace Altinn.App.Core.Internal.AltinnCdn;

internal sealed class AltinnCdnClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory;

    public AltinnCdnClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AltinnCdnOrgs> GetOrgs(CancellationToken cancellationToken = default)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        return await client.GetFromJsonAsync<AltinnCdnOrgs>(
                requestUri: "https://altinncdn.no/orgs/altinn-orgs.json",
                options: _jsonOptions,
                cancellationToken: cancellationToken
            ) ?? throw new JsonException("Received literal \"null\" response from Altinn CDN");
    }
}
