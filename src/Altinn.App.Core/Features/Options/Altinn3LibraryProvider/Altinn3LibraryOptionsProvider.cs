using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options.Altinn3LibraryProvider;

internal class Altinn3LibraryOptionsProvider : IAppOptionsProvider
{
    private readonly IAppOptionsService _appOptionsService;

    public Altinn3LibraryOptionsProvider(
        string optionId,
        string org,
        string codeListId,
        string? version,
        IAppOptionsService appOptionsService
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(org);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeListId);

        Id = optionId;
        _org = org;
        _codeListId = codeListId;
        _version = !string.IsNullOrEmpty(version) ? version : "latest";
        _appOptionsService = appOptionsService;
    }

    public string Id { get; }
    private readonly string _org;
    private readonly string _codeListId;
    private readonly string _version;

    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        var result = await _appOptionsService.GetCachableCodeListResponseAsync(_org, _codeListId, _version);
        return _appOptionsService.MapAppOptions(result, language);
    }
}
