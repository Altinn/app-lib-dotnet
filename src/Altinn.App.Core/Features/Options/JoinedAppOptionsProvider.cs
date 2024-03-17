using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Options;

/// <summary>
/// Utility class for joining multiple app options providers into one
/// </summary>
public class JoinedAppOptionsProvider : IAppOptionsProvider
{
    private readonly IEnumerable<string> _subOptions;
    private readonly Func<AppOptionsFactory> _appOptionsFactory;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">The option id used in layouts to reference this code list</param>
    /// <param name="subOptions">A list of other options to include</param>
    /// <param name="appOptionsFactory">A function that delays the initialization of the factory to use to get the sub options</param>
    public JoinedAppOptionsProvider(string id, IEnumerable<string> subOptions, Func<AppOptionsFactory> appOptionsFactory)
    {
        Id = id;
        _subOptions = subOptions;
        _appOptionsFactory = appOptionsFactory;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public async Task<AppOptions> GetAppOptionsAsync(string? language, Dictionary<string, string> keyValuePairs)
    {
        // Pretend this isn't the same as injecting the ServiceProvider
        var appOptionsFactory = _appOptionsFactory();
        // Get options for all subOptions ids
        var appOptions = await Task.WhenAll(
            _subOptions
                .Select(async optionId =>
                {
                    var p = appOptionsFactory.GetOptionsProvider(optionId);
                    return (p.Id, AppOption: await p.GetAppOptionsAsync(language, keyValuePairs));
                }));

        // Flatten all options to a single list
        var options = appOptions.SelectMany(o => o.AppOption.Options ?? Enumerable.Empty<AppOption>()).ToList();
        // Flatten all parameters to a single dictionary, prefixing the key with the option id
        var parameters = appOptions
            .SelectMany(o =>
                o.AppOption.Parameters.Select(p =>
                    new KeyValuePair<string, string?>($"{o.Id}_{p.Key}", p.Value)))
            .ToDictionary();

        // Return the combined AppOptions object
        return new AppOptions
        {
            IsCacheable = appOptions.All(o => o.AppOption.IsCacheable),
            Options = options,
            Parameters = parameters
        };
    }
}