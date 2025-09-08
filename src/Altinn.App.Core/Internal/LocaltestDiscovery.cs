using System.Collections.Frozen;
using Altinn.App.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal;

internal static class LocaltestDiscoveryDI
{
    public static IServiceCollection AddLocaltestDiscovery(this IServiceCollection services)
    {
        services.AddSingleton<LocaltestDiscovery>();
        return services;
    }
}

internal sealed class LocaltestDiscovery(
    IOptionsMonitor<GeneralSettings> _generalSettings,
    IOptionsMonitor<PlatformSettings> _platformSettings
)
{
    private static readonly FrozenSet<string> _expectedHostnames = new[]
    {
        "local.altinn.cloud",
        "altinn3local.no",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public bool IsRunning() => _expectedHostnames.Contains(_generalSettings.CurrentValue.HostName);

    public string GetBaseUrl() =>
        new Uri(_platformSettings.CurrentValue.ApiStorageEndpoint).GetLeftPart(UriPartial.Authority);
}
