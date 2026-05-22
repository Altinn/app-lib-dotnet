using System.Runtime.CompilerServices;

namespace Altinn.App.Api.Generated;

internal static class AltinnAppApiModuleInitializer
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Altinn.App.Api.Infrastructure.Telemetry.AppRequestRootPropagation.Install();
    }
}
