using Altinn.App.Core.Implementation;
using Altinn.App.Core.Internal.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.TokenProvider;

/// <summary>
/// Extension methods for the specific token provider.
/// </summary>
public static class SpecificTokenProviderExtensions
{
    /// <summary>
    /// This method is used to register the specific token provider in the service collection.
    /// Also removes the ITokenProvider if its already registered.
    /// Also Adds the UserTokenProvider as a transient service to use as default token provider
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection UseSpecificTokenProvider(this IServiceCollection services)
    {
        return services.Register<UserTokenProvider>();
    }

    /// <summary>
    /// This method is used to register the specific token provider in the service collection.
    /// With a specific token provider type to use as default provider.
    /// </summary>
    /// <typeparam name="TDefaultProvider">The Type of the ITokenProvider the specifictokenprovider will use as default</typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection UseSpecificTokenProvider<TDefaultProvider>(this IServiceCollection services)
        where TDefaultProvider : ITokenProvider
    {
        return services.Register<TDefaultProvider>();
    }

    private static IServiceCollection Register<TDefault>(this IServiceCollection services)
        where TDefault : ITokenProvider
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITokenProvider));

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddTransient(typeof(TDefault));

        services.AddSingleton<SpecificTokenProviderStateContext>();

        services.AddTransient<ITokenProvider, SpecificTokenProvider>(sp =>
        {
            return new SpecificTokenProvider(
                sp.GetRequiredService<TDefault>(),
                sp.GetRequiredService<SpecificTokenProviderStateContext>()
            );
        });
        return services;
    }
}
