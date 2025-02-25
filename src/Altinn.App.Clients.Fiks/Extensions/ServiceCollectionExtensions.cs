using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a <see cref="IFiksIOClient"/> service to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    public static IServiceCollection AddFiksIOClient(this IServiceCollection services)
    {
        // Only auto-add FiksIOSettings if not already configured.
        // Users sometimes wish to bind the default options to another configuration path than "FiksIOSettings".
        if (services.IsConfigured<FiksIOSettings>() is false)
            services.ConfigureFiksIOClient("FiksIOSettings");

        services.AddTransient<IFiksIOClient, FiksIOClient>();

        return services;
    }

    /// <summary>
    /// Binds a <see cref="FiksIOSettings"/> configuration to the supplied config section path and options name.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configSectionPath">The configuration section path, e.g. "MaskinportenSettingsInternal"</param>
    public static IServiceCollection ConfigureFiksIOClient(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<FiksIOSettings>().BindConfiguration(configSectionPath).ValidateDataAnnotations();

        return services;
    }
}
