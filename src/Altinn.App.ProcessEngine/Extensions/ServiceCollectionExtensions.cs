using Altinn.App.Core.Extensions;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Altinn.App.ProcessEngine.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcessEngine(this IServiceCollection services)
    {
        if (services.IsConfigured<ProcessEngineSettings>() is false)
        {
            services.ConfigureProcessEngine("ProcessEngineSettings");
        }

        services.AddSingleton<IProcessEngine, ProcessEngine>();
        services.AddSingleton<IProcessEngineTaskHandler, ProcessEngineTaskHandler>();
        services.AddHostedService<ProcessEngineHost>();

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthConstants.ApiKeySchemeName, null);

        services
            .AddControllers()
            .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(ApiKeyAuthenticationHandler).Assembly));

        return services;
    }

    public static IServiceCollection ConfigureProcessEngine(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<ProcessEngineSettings>().BindConfiguration(configSectionPath);

        return services;
    }

    public static IServiceCollection ConfigureProcessEngine(
        this IServiceCollection services,
        Action<ProcessEngineSettings> configureOptions
    )
    {
        services.AddOptions<ProcessEngineSettings>().Configure(configureOptions);
        return services;
    }
}
