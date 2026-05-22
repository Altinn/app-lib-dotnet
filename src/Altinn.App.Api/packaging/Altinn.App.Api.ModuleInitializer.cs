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
        var propagationType = typeof(Altinn.App.Api.Extensions.ServiceCollectionExtensions).Assembly.GetType(
            "Altinn.App.Api.Infrastructure.Telemetry.AppRequestRootPropagation",
            throwOnError: true
        )!;
        var installMethod =
            propagationType.GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new System.MissingMethodException(propagationType.FullName, "Install");

        installMethod.Invoke(null, null);
    }
}
