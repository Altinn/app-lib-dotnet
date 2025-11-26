using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Options.Altinn3LibraryProvider;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.App.Core.Features.Options;

/// <summary>
/// Service for handling app options aka code lists.
/// </summary>
public class AppOptionsService : IAppOptionsService
{
    private readonly AppOptionsFactory _appOptionsFactory;
    private readonly InstanceAppOptionsFactory _instanceAppOptionsFactory;
    private readonly Telemetry? _telemetry;
    private readonly IAltinn3LibraryCodeListApiClient _altinn3LibraryCodeListApiClient;
    private readonly HybridCache _hybridCache;

    private static readonly HybridCacheEntryOptions _defaultCacheExpiration = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AppOptionsService"/> class.
    /// </summary>
    public AppOptionsService(
        AppOptionsFactory appOptionsFactory,
        InstanceAppOptionsFactory instanceAppOptionsFactory,
        IAltinn3LibraryCodeListApiClient altinn3LibraryCodeListApiClient,
        HybridCache hybridCache,
        Telemetry? telemetry = null
    )
    {
        _appOptionsFactory = appOptionsFactory;
        _instanceAppOptionsFactory = instanceAppOptionsFactory;
        _altinn3LibraryCodeListApiClient = altinn3LibraryCodeListApiClient;
        _hybridCache = hybridCache;
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    public async Task<AppOptions> GetOptionsAsync(
        string optionId,
        string? language,
        Dictionary<string, string> keyValuePairs
    )
    {
        using var activity = _telemetry?.StartGetOptionsActivity();
        return await _appOptionsFactory.GetOptionsProvider(optionId).GetAppOptionsAsync(language, keyValuePairs);
    }

    /// <inheritdoc/>
    public async Task<AppOptions> GetOptionsAsync(
        InstanceIdentifier instanceIdentifier,
        string optionId,
        string? language,
        Dictionary<string, string> keyValuePairs
    )
    {
        using var activity = _telemetry?.StartGetOptionsActivity(instanceIdentifier);
        return await _instanceAppOptionsFactory
            .GetOptionsProvider(optionId)
            .GetInstanceAppOptionsAsync(instanceIdentifier, language, keyValuePairs);
    }

    /// <inheritdoc/>
    public async Task<Altinn3LibraryCodeListResponse> GetCachableCodeListResponseAsync(
        string org,
        string codeListId,
        string? version
    )
    {
        version = !string.IsNullOrEmpty(version) ? version : "latest";
        var result = await _hybridCache.GetOrCreateAsync(
            $"Altinn3Library:{org}-{codeListId}-{version}",
            async cancellationToken =>
                await _altinn3LibraryCodeListApiClient.GetAltinn3LibraryCodeLists(
                    org,
                    codeListId,
                    version,
                    cancellationToken
                ),
            options: _defaultCacheExpiration
        );

        return result;
    }

    /// <inheritdoc/>
    public AppOptions MapAppOptions(Altinn3LibraryCodeListResponse libraryCodeLists, string? language)
    {
        var options = libraryCodeLists
            .Codes.Select(code => new AppOption
            {
                Value = code.Value,
                Label = GetValueWithLanguageFallback(code.Label, language),
                Description = GetValueWithLanguageFallback(code.Description, language),
                HelpText = GetValueWithLanguageFallback(code.HelpText, language),
            })
            .ToList();

        return new AppOptions
        {
            IsCacheable = true,
            Options = options,
            Parameters = new Dictionary<string, string?>
            {
                { "version", libraryCodeLists.Version },
                { "source", libraryCodeLists.Source.Name },
            },
        };
    }

    /// <inheritdoc/>
    public OrgLibraryAppOptions MapOrgLibraryAppOptions(
        Altinn3LibraryCodeListResponse libraryCodeLists,
        string? language
    )
    {
        var options = libraryCodeLists
            .Codes.Select(code => new OrgLibraryAppOption()
            {
                Value = code.Value,
                Label = GetValueWithLanguageFallback(code.Label, language),
                Description = GetValueWithLanguageFallback(code.Description, language),
                HelpText = GetValueWithLanguageFallback(code.HelpText, language),
                Tags = code.Tags,
            })
            .ToList();

        return new OrgLibraryAppOptions
        {
            Name = libraryCodeLists.Source.Name,
            Options = options,
            TagNames = libraryCodeLists.TagNames,
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
