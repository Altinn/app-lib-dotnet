using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options.Altinn3LibraryCodeList;

/// <summary>
/// Service for handling Altinn 3 library code lists.
/// </summary>
public interface IAltinn3LibraryCodeListService
{
    /// <summary>
    /// Gets cached code lists from <see cref="GetCachedCodeListResponseAsync"/> and maps the response to AppOptions by calling <see cref="MapAppOptions"/>.
    /// </summary>
    /// <param name="org">Creator organization</param>
    /// <param name="codeListId">Code list id</param>
    /// <param name="version">Code list version</param>
    /// <param name="language">Preferred language to map to. Has fallback, will try to map to requested language, else Nb, En, then first available (alphabetically by key) if not provided or not found.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>App options</returns>
    Task<AppOptions> GetLibraryCodeListOptionsAsync(
        string org,
        string codeListId,
        string version,
        string? language,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Getting code list from Altinn3 library, caching the result if not already cached.
    /// </summary>
    /// <param name="org">Creator organization</param>
    /// <param name="codeListId">Code list id</param>
    /// <param name="version">Code list version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Altinn 3 library code list response</returns>
    Task<Altinn3LibraryCodeListResponse> GetCachedCodeListResponseAsync(
        string org,
        string codeListId,
        string? version,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Mapping Altinn3 library code list response to AppOptions
    /// </summary>
    /// <param name="libraryCodeListResponse">Code list input</param>
    /// <param name="language">Preferred language to map to. Has fallback, will try to map to requested language, else Nb, En, then first available (alphabetically by key) if not provided or not found.</param>
    /// <returns>App options</returns>
    AppOptions MapAppOptions(Altinn3LibraryCodeListResponse libraryCodeListResponse, string? language);
}
