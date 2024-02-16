using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.Options;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> for adding app options providers
/// </summary>
public static class AppOptionsServiceExtentions
{
    /// <summary>
    /// Join multiple app options providers into one
    /// </summary>
    public static void AddJoinedAppOptions(this IServiceCollection services, string id, params string[] subLists)
    {
        services.AddTransient<IAppOptionsProvider>(sp =>
            new JoinedAppOptionsProvider(
                id,
                subLists,
                sp.GetRequiredService<AppOptionsFactory>));
    }
}