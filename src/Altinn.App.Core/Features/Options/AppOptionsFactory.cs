using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.Options;

/// <summary>
/// Factory class for resolving <see cref="IAppOptionsProvider"/> implementations
/// based on the name/id of the app options requested.
/// </summary>
public sealed class AppOptionsFactory
{
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IServiceProvider _serviceProvider;

    private const string DefaultProviderName = "default";

    /// <summary>
    /// Initializes a new instance of the <see cref="AppOptionsFactory"/> class.
    /// </summary>
    public AppOptionsFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    /// <summary>
    /// Finds the implementation of IAppOptionsProvider based on the options id
    /// provided.
    /// </summary>
    /// <param name="optionsId">Id matching the options requested.</param>
    public IAppOptionsProvider GetOptionsProvider(string optionsId)
    {
        var appOptionsProviders = _appImplementationFactory.GetAll<IAppOptionsProvider>();
        foreach (var appOptionProvider in appOptionsProviders)
        {
            if (!appOptionProvider.Id.Equals(optionsId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return appOptionProvider;
        }

        if (optionsId != DefaultProviderName)
        {
            return new DefaultAppOptionsProvider(_serviceProvider) { Id = optionsId };
        }

        throw new KeyNotFoundException(
            "No app options provider found in the configured services. Please check your services configuration."
        );
    }
}
