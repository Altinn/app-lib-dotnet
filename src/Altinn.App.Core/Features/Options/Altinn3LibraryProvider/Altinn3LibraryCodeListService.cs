using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.App.Core.Features.Options.Altinn3LibraryProvider;

/// <summary>
/// Service for handling Altinn 3 library code lists.
/// </summary>
internal sealed class Altinn3LibraryCodeListService : IAltinn3LibraryCodeListService
{
    private readonly HybridCache _hybridCache;
    private readonly IAltinn3LibraryCodeListApiClient _altinn3LibraryCodeListApiClient;

    private static readonly HybridCacheEntryOptions _defaultCacheExpiration = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="Altinn3LibraryCodeListService"/> class.
    /// </summary>
    public Altinn3LibraryCodeListService(
        HybridCache hybridCache,
        IAltinn3LibraryCodeListApiClient altinn3LibraryCodeListApiClient
    )
    {
        _hybridCache = hybridCache;
        _altinn3LibraryCodeListApiClient = altinn3LibraryCodeListApiClient;
    }

    /// <inheritdoc/>
    public async Task<Altinn3LibraryCodeListResponse> GetCachedCodeListResponseAsync(
        string org,
        string codeListId,
        string? version,
        CancellationToken cancellationToken
    )
    {
        version = !string.IsNullOrEmpty(version) ? version : "latest";
        var result = await _hybridCache.GetOrCreateAsync(
            $"Altinn3Library:{org}-{codeListId}-{version}",
            async cancel =>
                await _altinn3LibraryCodeListApiClient.GetAltinn3LibraryCodeLists(org, codeListId, version, cancel),
            options: _defaultCacheExpiration,
            cancellationToken: cancellationToken
        );

        return result;
    }

    /// <inheritdoc/>
    public AppOptions MapAppOptions(Altinn3LibraryCodeListResponse libraryCodeListResponse, string? language)
    {
        var options = libraryCodeListResponse
            .Codes.Select(code =>
            {
                Dictionary<string, string>? tagDict = null;
                if (
                    (code.Tags is not null && libraryCodeListResponse.TagNames is not null)
                    && code.Tags.Count == libraryCodeListResponse.TagNames.Count
                )
                {
                    tagDict = libraryCodeListResponse
                        .TagNames.Select((k, index) => new { k, v = code.Tags[index] })
                        .ToDictionary(x => x.k, x => x.v);
                }
                return new AppOption
                {
                    Value = code.Value,
                    Label = GetValueWithLanguageFallback(code.Label, language),
                    Description = GetValueWithLanguageFallback(code.Description, language),
                    HelpText = GetValueWithLanguageFallback(code.HelpText, language),
                    Tags = tagDict,
                };
            })
            .ToList();

        return new AppOptions
        {
            IsCacheable = true,
            Options = options,
            Parameters = new Dictionary<string, string?>
            {
                { "version", libraryCodeListResponse.Version },
                { "source", libraryCodeListResponse.Source.Name },
            },
        };
    }

    /// <summary>
    /// Gets a value from a language collection with fallback logic.
    /// Attempts to find a value in this order: requested language, Nb, En, then first available (alphabetically by key).
    /// </summary>
    [return: NotNullIfNotNull(nameof(languageCollection))]
    private static string? GetValueWithLanguageFallback(
        Dictionary<string, string>? languageCollection,
        string? language
    )
    {
        if (languageCollection == null)
        {
            return null;
        }
        if (languageCollection.Count == 0)
        {
            return string.Empty;
        }
        if (
            language != null && languageCollection.TryGetValue(language, out var value)
            || languageCollection.TryGetValue(LanguageConst.Nb, out value)
            || languageCollection.TryGetValue(LanguageConst.En, out value)
        )
        {
            return value;
        }

        return languageCollection.OrderBy(x => x.Key).First().Value;
    }
}
