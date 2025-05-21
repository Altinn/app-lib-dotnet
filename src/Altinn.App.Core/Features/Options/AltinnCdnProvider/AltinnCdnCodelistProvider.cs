using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options.AltinnCdnProvider;

/// <summary>
/// Provides code lists created by service owners from Altinn CDN.
/// </summary>
public class AltinnCdnCodelistProvider : IAppOptionsProvider
{
    private readonly IAltinnCdnApiClient _client;

    /// <summary>
    /// Initialises a new instance of the <see cref="AltinnCdnCodelistProvider"/> class.
    /// </summary>
    public AltinnCdnCodelistProvider(IAltinnCdnApiClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public string Id => "altinn-cdn";

    /// <inheritdoc />
    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        ArgumentNullException.ThrowIfNull(language);
        EnsureRequiredKeys(keyValuePairs, "orgName", "codeListId", "version");

        var codeList = await _client.GetCodeList(
            keyValuePairs["orgName"],
            keyValuePairs["codeListId"],
            keyValuePairs["version"],
            language
        );

        return new AppOptions { Options = codeList };
    }

    private static void EnsureRequiredKeys(Dictionary<string, string> dict, params string[] requiredKeys)
    {
        foreach (var key in requiredKeys)
        {
            if (!dict.ContainsKey(key))
            {
                throw new ArgumentException($"The key '{key}' is missing in keyValuePairs.", nameof(dict));
            }
        }
    }
}
