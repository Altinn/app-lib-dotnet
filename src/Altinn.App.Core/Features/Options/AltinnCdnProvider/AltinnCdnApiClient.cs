using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options.AltinnCdnProvider;

/// <inheritdoc />
public class AltinnCdnApiClient : IAltinnCdnApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates an instance of <see cref="AltinnCdnApiClient"/>.
    /// </summary>
    public AltinnCdnApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<List<AppOption>> GetCodeList(string orgName, string codeListId, string version, string language)
    {
        var url = $"https://altinncdn.no/orgs/{orgName}/codelists/{codeListId}/{version}/{language}.json";
        var response = await _httpClient.GetAsync(url);
        return await JsonSerializerPermissive.DeserializeAsync<List<AppOption>>(response.Content);
    }
}
