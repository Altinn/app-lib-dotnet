using System.Net;
using Altinn.App.Core.Helpers;

namespace Altinn.App.Core.Features.Options.Altinn2Provider
{
    /// <summary>
    /// HttpClientWrapper for the altinn2 metadata/codelists api
    /// </summary>
    public class Altinn2MetadataApiClient
    {
        /// <summary>
        /// HttpClient
        /// </summary>
        private readonly HttpClient _client;

        /// <summary>
        /// Constructor
        /// </summary>
        public Altinn2MetadataApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Fetch the code list
        /// </summary>
        /// <param name="id">id of the code list</param>
        /// <param name="langCode">Language code per altinn2 definisions (nb=>1044, ...)</param>
        /// <param name="version">The version number for the list in the api</param>
        public async Task<MetadataCodelistResponse> GetAltinn2Codelist(string id, string langCode, int? version = null)
        {
            return await Get(id, langCode, version) ?? await Get(id, langCode: null, version);

            async Task<MetadataCodelistResponse?> Get(string id, string? langCode, int? version)
            {
                var versionParam = version?.ToString() ?? "";
                var langCodeParam = langCode is null ? "" : $"?language={langCode}";
                var url = $"https://www.altinn.no/api/metadata/codelists/{id}/{versionParam}{langCodeParam}";
                using var response = await _client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await JsonSerializerPermissive.DeserializeAsync<MetadataCodelistResponse>(response.Content);
            }
        }
    }
}
