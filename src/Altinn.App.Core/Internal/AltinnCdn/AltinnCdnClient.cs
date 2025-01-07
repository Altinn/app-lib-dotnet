using System.Net.Http.Json;
using System.Text.Json;

namespace Altinn.App.Core.Internal.AltinnCdn;

internal sealed class AltinnCdnClient : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpMessageHandler? _httpMessageHandler;

    public AltinnCdnClient(HttpMessageHandler? httpMessageHandler = null)
    {
        _httpMessageHandler = httpMessageHandler;
    }

    public async Task<AltinnCdnOrgs> GetOrgs(CancellationToken cancellationToken = default)
    {
        using var client = CreateHttpClient();

        AltinnCdnOrgs orgs =
            await client.GetFromJsonAsync<AltinnCdnOrgs>(
                requestUri: "https://altinncdn.no/orgs/altinn-orgs.json",
                options: _jsonOptions,
                cancellationToken: cancellationToken
            ) ?? throw new JsonException("Received literal \"null\" response from Altinn CDN");

        // Inject Digdir's organisation number for TTD, because TTD does not have an organisation number
        if (
            !orgs.Orgs.IsNullOrEmpty()
            && orgs.Orgs.TryGetValue("ttd", out var ttdOrgDetails)
            && orgs.Orgs.TryGetValue("digdir", out var digdirOrgDetails)
            && string.IsNullOrEmpty(ttdOrgDetails.Orgnr)
        )
        {
            ttdOrgDetails.Orgnr = digdirOrgDetails.Orgnr;
        }

        return orgs;
    }

    private HttpClient CreateHttpClient()
    {
        return _httpMessageHandler is not null ? new HttpClient(_httpMessageHandler) : new HttpClient();
    }

    public void Dispose()
    {
        _httpMessageHandler?.Dispose();
    }
}
