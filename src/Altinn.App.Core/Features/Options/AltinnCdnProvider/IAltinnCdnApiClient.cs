using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options.AltinnCdnProvider;

/// <summary>
/// Client for getting code lists from Altinn CDN.
/// </summary>
public interface IAltinnCdnApiClient
{
    /// <summary>
    /// Fetches a code list from Altinn CDN.
    /// </summary>
    public Task<List<AppOption>> GetCodeList(string orgName, string codeListId, string version, string language);
}
