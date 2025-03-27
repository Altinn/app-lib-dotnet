using System.Runtime.CompilerServices;

namespace Altinn.App.Shared.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.AutoVerify(includeBuildServer: false);
    }
}
