using System.Reflection;
using System.Runtime.CompilerServices;

namespace Altinn.App.Api.Generated;

internal static class AltinnAppApiModuleInitializer
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        typeof(Altinn.App.Api.Extensions.ServiceCollectionExtensions)
            .Assembly.GetType("Altinn.App.Api.Infrastructure.Telemetry.AppRequestRootPropagation", throwOnError: true)!
            .GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, null);
    }
}
