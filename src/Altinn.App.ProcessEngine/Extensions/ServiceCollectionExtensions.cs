using Altinn.App.Core.Extensions;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Extensions;

internal static class ServiceCollectionExtensions
{
    // TODO: Add builder pattern like for FiksIO client

    public static IServiceCollection AddProcessEngine(this IServiceCollection services)
    {
        if (services.IsConfigured<ProcessEngineSettings>() is false)
        {
            services.ConfigureProcessEngine("ProcessEngineSettings");
        }

        services.AddSingleton<IProcessEngine, ProcessEngine>();
        services.AddSingleton<IProcessEngineTaskHandler, ProcessEngineTaskHandler>();
        services.AddHostedService<ProcessEngineHost>();

        return services;
    }

    public static IServiceCollection ConfigureProcessEngine(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<ProcessEngineSettings>().BindConfiguration(configSectionPath);

        return services;
    }
}
