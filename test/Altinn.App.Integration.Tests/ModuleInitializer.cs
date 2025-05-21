using System.Runtime.CompilerServices;

namespace Altinn.App.Integration.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.AutoVerify(
            (_, _, _) => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")),
            includeBuildServer: true // Don't use built in detection, it says WSL is also a build server
        );
        VerifyAspNetCore.Initialize();
    }
}
