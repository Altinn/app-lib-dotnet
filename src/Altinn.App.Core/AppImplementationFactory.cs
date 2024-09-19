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
        services.AddSingleton<AppImplementationFactory.DIScopeHolder>(sp =>
            new(sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestServices)
        );
        services.AddSingleton<AppImplementationFactory>();
        return services;
    }

    public static IServiceCollection AddTestAppImplementationFactory(this IServiceCollection services)
    {
        services.AddSingleton<AppImplementationFactory.DIScopeHolder>(sp => new(sp));
        services.AddSingleton<AppImplementationFactory>();
        return services;
    }
}

internal sealed class AppImplementationFactory
{
    internal sealed record DIScopeHolder(IServiceProvider? ScopedServiceProvider);

    private readonly DIScopeHolder _diScopeHolder;

    public AppImplementationFactory(DIScopeHolder diScopeHolder)
    {
        _diScopeHolder = diScopeHolder;
    }

    private IServiceProvider _sp =>
        // Right now we are just using the HttpContext to get the current scope,
        // in the future we might not be always running in a web context,
        // at that point we need to replace this
        _diScopeHolder.ScopedServiceProvider
        ?? throw new InvalidOperationException("Couldn't resolve DI container from current context");

    public T GetRequired<T>()
        where T : class => _sp.GetRequiredService<T>();

    public T? Get<T>()
        where T : class => _sp.GetService<T>();

    public IEnumerable<T> GetAll<T>()
        where T : class => _sp.GetServices<T>();
}
