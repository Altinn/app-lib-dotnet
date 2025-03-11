using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.App.Clients.Fiks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IFiksIOSetupBuilder AddFiksIOClient(this IServiceCollection services)
    {
        if (services.IsConfigured<FiksIOSettings>() is false)
            services.ConfigureFiksIOClient("FiksIOSettings");

        services.AddTransient<IFiksIOClient, FiksIOClient>();

        return new FiksIOSetupBuilder(services);
    }

    public static IFiksArkivSetupBuilder AddFiksArkiv(this IServiceCollection services)
    {
        if (services.IsConfigured<FiksArkivSettings>() is false)
        {
            services.ConfigureFiksArkiv("FiksArkivSettings");
        }

        services.AddFiksIOClient();
        // services.AddTransient<IFiksArkivErrorHandler, FiksArkivDefaultErrorHandler>();
        services.AddTransient<IFiksArkivMessageProvider, FiksArkivDefaultMessageProvider>();
        services.AddTransient<IFiksArkivServiceTask, FiksArkivServiceTask>();
        services.AddTransient<IServiceTask>(x => x.GetRequiredService<IFiksArkivServiceTask>());
        services.AddHostedService<FiksArkivConfigValidationService>();
        services.AddHostedService<FiksArkivEventService>();

        return new FiksArkivSetupBuilder(services);
    }

    public static IServiceCollection ConfigureFiksIOClient(
        this IServiceCollection services,
        Action<FiksIOSettings> configureOptions
    )
    {
        services.AddOptions<FiksIOSettings>().Configure(configureOptions).ValidateDataAnnotations();
        return services;
    }

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
}
