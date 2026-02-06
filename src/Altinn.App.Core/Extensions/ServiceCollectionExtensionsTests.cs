using Altinn.App.Core.Internal.WorkflowEngine.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Extensions;

/// <summary>
/// Extension methods for registering test implementations of services.
/// </summary>
public static class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Adds an in-memory lock-scoped instance cache for testing.
    /// Thread-safe and uses ConcurrentDictionary for storage.
    /// </summary>
    public static IServiceCollection AddInMemoryLockScopedInstanceCache(this IServiceCollection services)
    {
        services.AddSingleton<ILockScopedInstanceCache, InMemoryLockScopedInstanceCache>();
        return services;
    }
}
