using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Options.Altinn3LibraryProvider;

internal class Altinn3LibraryOptionsProvider : IAppOptionsProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlatformSettings _platformSettings;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private byte[]? _appOptionsCache;

    private DateTimeOffset _cacheExpiration = DateTimeOffset.MinValue;

    // Consider making this configurable via options, but there does not seem to be a strong use case
    private static readonly TimeSpan _cacheDurationSuccess = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan _cacheDurationFailure = TimeSpan.FromMinutes(1);

    private bool _isFetching;

    public Altinn3LibraryOptionsProvider(
        string optionId,
        string org,
        string codeListId,
        string? version,
        IHttpClientFactory httpClientFactory,
        IOptions<PlatformSettings> platformSettings
    )
    {
        _httpClientFactory = httpClientFactory;
        Id = optionId;
        _org = org;
        _codeListId = codeListId;
        _version = !string.IsNullOrEmpty(version) ? version : "latest";
        _platformSettings = platformSettings.Value;
    }

    public string Id { get; }
    private string _org { get; }
    private string _codeListId { get; }
    private string _version { get; }

    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        if (_appOptionsCache is not null && (_cacheExpiration > DateTimeOffset.UtcNow || _isFetching))
        {
            // there is a cached value and it is either valid or another thread is fetching an updated value
            return ParseAppOptionsFromCache(language);
        }

        await _cacheLock.WaitAsync();
        try
        {
            _isFetching = true;
            _appOptionsCache = await FetchAppOptionsFromAltinn3Library();
            _cacheExpiration = DateTimeOffset.UtcNow.Add(_cacheDurationSuccess);
            return ParseAppOptionsFromCache(language);
        }
        catch (Exception ex)
        {
            // Log the exception, but return cached value if available

            if (_appOptionsCache is not null)
            {
                _cacheExpiration = DateTimeOffset.UtcNow.Add(_cacheDurationFailure);
                return ParseAppOptionsFromCache(language);
            }
            throw;
        }
        finally
        {
            _isFetching = false;
            _cacheLock.Release();
        }
    }

    private AppOptions ParseAppOptionsFromCache(string? language)
    {
        var codeListRoot =
            JsonSerializer.Deserialize<Altinn3LibraryCodeListRoot>(_appOptionsCache)
            ?? throw new UnreachableException("Cached app options was \"null\"");

        var options = codeListRoot
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
                { "version", codeListRoot.Version },
                { "source", codeListRoot.Source.Name },
            },
        };
    }

    [return: NotNullIfNotNull(nameof(values))]
    private static string? GetValueWithLanguageFallback(Dictionary<string, string>? values, string? language)
    {
        if (values == null)
        {
            return null;
        }
        if (values.Count == 0)
        {
            return string.Empty;
        }
        if (language != null && values.TryGetValue(language, out var value))
        {
            return value;
        }
        if (values.TryGetValue(LanguageConst.Nb, out value))
        {
            return value;
        }
        if (values.TryGetValue(LanguageConst.En, out value))
        {
            return value;
        }

        return values.Values.First();
    }

    private async Task<byte[]> FetchAppOptionsFromAltinn3Library()
    {
        var client = _httpClientFactory.CreateClient("Altinn3LibraryClient");
        client.BaseAddress = new Uri(_platformSettings.Altinn3LibraryApiEndpoint);
        var response = await client.GetAsync($"{_org}/code_lists/{_codeListId}/{_version}.json");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}

public class Altinn3LibraryCodeListRoot
{
    [JsonPropertyName("codes")]
    public required List<Altinn3LibraryCodeListItem> Codes { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("source")]
    public required Altinn3LibraryCodeListSource Source { get; set; }

    [JsonPropertyName("tagNames")]
    public required List<string> TagNames { get; set; }
}

public class Altinn3LibraryCodeListSource
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class Altinn3LibraryCodeListItem
{
    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("label")]
    public required Dictionary<string, string> Label { get; set; }

    [JsonPropertyName("description")]
    public Dictionary<string, string>? Description { get; set; }

    [JsonPropertyName("helpText")]
    public Dictionary<string, string>? HelpText { get; set; }

    [JsonPropertyName("tags")]
    public required List<string> Tags { get; set; }
}
