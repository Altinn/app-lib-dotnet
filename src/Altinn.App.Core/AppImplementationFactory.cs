using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features;

/// <summary>
/// Marker attribute for interfaces that are meant to be implemented by apps.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
internal sealed class ImplementableByAppsAttribute : Attribute { }

internal static class AppImplementationFactoryExtensions
{
    public static IServiceCollection AddAppImplementationFactory(this IServiceCollection services)
    {
        services.AddSingleton<AppImplementationFactory>(sp => new(sp, testMode: false));
        return services;
    }

    public static IServiceCollection AddTestAppImplementationFactory(this IServiceCollection services)
    {
        services.AddSingleton<AppImplementationFactory>(sp => new(sp, testMode: true));
        return services;
    }
}

internal sealed class AppImplementationFactory
{
    private readonly Func<IServiceProvider?> _getServiceProvider;

    public AppImplementationFactory(IServiceProvider sp, bool testMode)
    {
        if (testMode)
            _getServiceProvider = () => sp;
        else
            // Right now we are just using the HttpContext to get the current scope,
            // in the future we might not be always running in a web context,
            // at that point we need to replace this
            _getServiceProvider = () => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestServices;
    }

    private IServiceProvider _sp =>
        _getServiceProvider()
        ?? throw new InvalidOperationException("Couldn't resolve DI container from current context");

    public T GetRequired<T>()
        where T : class => _sp.GetRequiredService<T>();

    public T? Get<T>()
        where T : class => _sp.GetService<T>();

    public IEnumerable<T> GetAll<T>()
        where T : class => _sp.GetServices<T>();
}
