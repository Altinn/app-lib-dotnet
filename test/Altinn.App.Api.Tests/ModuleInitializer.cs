using System.Runtime.CompilerServices;
using DiffEngine;

namespace Altinn.App.Api.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.InitializePlugins();
        if (BuildServerDetector.Detected && BuildServerDetector.IsWsl)
            BuildServerDetector.Detected = false; // WSL is not a build server
        VerifierSettings.AutoVerify(includeBuildServer: false);
    }
}
