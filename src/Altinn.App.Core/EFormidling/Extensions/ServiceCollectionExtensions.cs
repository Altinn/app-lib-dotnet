using Altinn.App.Core.EFormidling.Implementation;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.Common.EFormidlingClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.EFormidling.Extensions;

public static class ServiceCollectionExtensions
{
    
        
    public static void AddEFormidlingServices<TM>(this IServiceCollection services, IConfiguration configuration) where TM: IEFormidlingMetadata
    {
        services.AddHttpClient<IEFormidlingClient, Altinn.Common.EFormidlingClient.EFormidlingClient>();
        services.AddTransient<IEFormidlingReceivers, DefaultEFormidlingReceivers>();
        services.AddTransient<IEFormidlingService, DefaultEFormidlingService>();
        services.Configure<Altinn.Common.EFormidlingClient.Configuration.EFormidlingClientSettings>(configuration.GetSection("EFormidlingClientSettings"));
        services.AddTransient(typeof(IEFormidlingMetadata), typeof(TM));
    }
    
    public static void AddEFormidlingServices<TM, TR>(this IServiceCollection services, IConfiguration configuration) where TM: IEFormidlingMetadata where TR: IEFormidlingReceivers
    {
        services.AddHttpClient<IEFormidlingClient, Altinn.Common.EFormidlingClient.EFormidlingClient>();
        services.AddTransient(typeof(IEFormidlingReceivers), typeof(TR));
        services.AddTransient<IEFormidlingService, DefaultEFormidlingService>();
        services.Configure<Altinn.Common.EFormidlingClient.Configuration.EFormidlingClientSettings>(configuration.GetSection("EFormidlingClientSettings"));
        services.AddTransient(typeof(IEFormidlingMetadata), typeof(TM));
    }
}