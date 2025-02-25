using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App;

public static class ServiceCollectionExtensionsPublic
{
    /// <summary>
    /// <p>Configures the <see cref="IFiksIOClient"/> service with a configuration object which will be static for the lifetime of the service.</p>
    /// <p>If you have already provided a <see cref="FiksIOSettings"/> configuration, either manually or
    /// implicitly via <see cref="WebHostBuilderExtensions.ConfigureAppWebHost"/>, this will be overridden.</p>
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">
    /// Action delegate that provides <see cref="FiksIOSettings"/> configuration for the <see cref="IFiksIOClient"/> service
    /// </param>
    public static IServiceCollection ConfigureFiksIOClient(
        this IServiceCollection services,
        Action<FiksIOSettings> configureOptions
    )
    {
        services.AddOptions<FiksIOSettings>().Configure(configureOptions).ValidateDataAnnotations();

        return services;
    }

    /// <summary>
    /// <p>Binds a <see cref="FiksIOSettings"/> configuration to the supplied config section path.</p>
    /// <p>If you have already provided a <see cref="IFiksIOClient"/> configuration, either manually or
    /// implicitly via <see cref="WebHostBuilderExtensions.ConfigureAppWebHost"/>, this will be overridden.</p>
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configSectionPath">The configuration section path (Eg. "FiksIOSettings")</param>
    public static IServiceCollection ConfigureFiksIOClient(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<FiksIOSettings>().BindConfiguration(configSectionPath).ValidateDataAnnotations();

        return services;
    }

    public static IServiceCollection ConfigureFiksArkiv(
        this IServiceCollection services,
        Action<FiksArkivSettings> configureOptions
    )
    {
        services.AddOptions<FiksArkivSettings>().Configure(configureOptions);

        return services;
    }

    public static IServiceCollection ConfigureFiksArkiv(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<FiksArkivSettings>().BindConfiguration(configSectionPath);

        return services;
    }

    public static IFiksArkivSetupBuilder AddFiksArkiv(this IServiceCollection services)
    {
        // // TODO: Move to AddAltinnAppServices. Client should be globally available regardless of FiksArkiv usage.
        // // Only auto-add FiksIOSettings if not already configured.
        // if (services.IsConfigured<FiksIOSettings>() is false)
        // {
        //     services.ConfigureFiksIOClient("FiksIOSettings");
        // }
        // services.AddTransient<IFiksIOClient, FiksIOClient>();



        // Only auto-add FiksArkivSettings if not already configured.
        if (services.IsConfigured<FiksArkivSettings>() is false)
        {
            services.ConfigureFiksArkiv("FiksArkivSettings");
        }

        services.AddTransient<IFiksArkivErrorHandler, FiksArkivDefaultErrorHandler>();
        services.AddTransient<IFiksArkivMessageProvider, FiksArkivDefaultMessageProvider>();
        services.AddTransient<IFiksArkivServiceTask, FiksArkivServiceTask>();
        services.AddHostedService<FiksArkivConfigurationValidator>();
        services.AddHostedService<FiksArkivEventService>();

        return new FiksArkivSetupBuilder(services);
    }
}
