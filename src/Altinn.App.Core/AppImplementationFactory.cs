using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features;

internal static class AppImplementationFactoryExtensions
{
    public static IServiceCollection AddAppImplementationFactory(this IServiceCollection services)
    {
        // Wherever we inject user/app-implemented interfaces,
        // we should be in a context where there is a scope
        // * ASP.NET Core request scope
        // * Background processing scope (i.e. IServiceTask in the future)
        // If code tries to resolve an implementation outside of a scope,
        // it will throw an exception.
        services.AddScoped<AppImplementationFactory>();
        return services;
    }

    public static T GetRequiredAppImplementation<T>(this IServiceProvider sp)
        where T : class => sp.GetRequiredService<AppImplementationFactory>().GetRequiredImplementation<T>();

    public static T? GetAppImplementation<T>(this IServiceProvider sp)
        where T : class => sp.GetRequiredService<AppImplementationFactory>().GetImplementation<T>();

    public static IEnumerable<T> GetAppImplementations<T>(this IServiceProvider sp)
        where T : class => sp.GetRequiredService<AppImplementationFactory>().GetImplementations<T>();
}

file sealed class AppImplementationFactory
{
    private readonly IServiceProvider _sp;

    public AppImplementationFactory(IServiceProvider serviceProvider)
    {
        _sp = serviceProvider;
    }

    public T GetRequiredImplementation<T>()
        where T : class => _sp.GetRequiredService<T>();

    public T? GetImplementation<T>()
        where T : class => _sp.GetService<T>();

    public IEnumerable<T> GetImplementations<T>()
        where T : class => _sp.GetServices<T>();
}
