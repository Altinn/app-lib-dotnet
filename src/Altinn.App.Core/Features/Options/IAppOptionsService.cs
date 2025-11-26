using Altinn.App.Core.Features.Options.Altinn3LibraryProvider;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options;

/// <summary>
/// Interface for working with <see cref="AppOption"/>
/// </summary>
public interface IAppOptionsService
{
    /// <summary>
    /// Get the list of options for a specific options list by its id and key/value pairs.
    /// </summary>
    /// <param name="optionId">The id of the options list to retrieve</param>
    /// <param name="language">The language code requested.</param>
    /// <param name="keyValuePairs">Optional list of key/value pairs to use for filtering and further lookup.</param>
    /// <returns>The list of options</returns>
    Task<AppOptions> GetOptionsAsync(string optionId, string? language, Dictionary<string, string> keyValuePairs);

    /// <summary>
    /// Get the list of instance specific options for a specific options list based on the <see cref="InstanceIdentifier"/>
    /// and key/value pairs. The values returned from this implementation could be specific to the instance and/or
    /// instance owner and should not be cached without careful thinking around caching strategy.
    /// </summary>
    /// <param name="instanceIdentifier">Class identifying the instance by instance owner party id and instance guid.</param>
    /// <param name="optionId">The id of the options list to retrieve</param>
    /// <param name="language">The language code requested.</param>
    /// <param name="keyValuePairs">Optional list of key/value pairs to use for filtering and further lookup.</param>
    /// <returns>The list of options</returns>
    Task<AppOptions> GetOptionsAsync(
        InstanceIdentifier instanceIdentifier,
        string optionId,
        string? language,
        Dictionary<string, string> keyValuePairs
    );

    /// <summary>
    /// Getting code list from Altinn3 library, caching the result if not already cached.
    /// </summary>
    /// <param name="org">The creating organization</param>
    /// <param name="codeListId">Code list id</param>
    /// <param name="version">Code list version</param>
    /// <returns>Altinn 3 library code list response</returns>
    Task<Altinn3LibraryCodeListResponse> GetCachableCodeListResponseAsync(
        string org,
        string codeListId,
        string? version
    );

    /// <summary>
    /// Mapping Altinn3 library code list response to AppOptions
    /// </summary>
    /// <param name="libraryCodeLists">Code list input</param>
    /// <param name="language">Prefered language to map to. Has fallback, will try to map to requested language, else Nb, En, then first available (alphabetically by key) if not provided or not found.</param>
    /// <returns>App options</returns>
    AppOptions MapAppOptions(Altinn3LibraryCodeListResponse libraryCodeLists, string? language);

    /// <summary>
    /// Mapping Altinn3 library code list response to OrgLibraryAppOptions
    /// </summary>
    /// <param name="libraryCodeLists">Code list input</param>
    /// <param name="language">Prefered language to map to. Has fallback, will try to map to requested language, else Nb, En, then first available (alphabetically by key) if not provided or not found.</param>
    /// <returns>Org library app options</returns>
    OrgLibraryAppOptions MapOrgLibraryAppOptions(Altinn3LibraryCodeListResponse libraryCodeLists, string? language);
}
