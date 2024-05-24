using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Altinn.App.Analyzers.Tests;

public class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        var testProjectDir = GetTestProjectDirectory();
        var path = Path.Combine(testProjectDir.FullName, "testapp", "App.sln");
        Assert.True(File.Exists(path));
        path = Path.GetFullPath(path);
        Assert.True(File.Exists(path));
        var testAppDirectory = Path.GetDirectoryName(path);
        Assert.NotNull(testAppDirectory);
        Assert.True(Directory.Exists(testAppDirectory));
        Directory.SetCurrentDirectory(testAppDirectory);

        InnerVerifier.ThrowIfVerifyHasBeenRun();
        VerifierSettings.AddExtraSettings(serializer =>
        {
            var converters = serializer.Converters;
            converters.Add(new DiagnosticJsonConverter());
        });
        Verifier.UseProjectRelativeDirectory("_snapshots");
    }

    private static DirectoryInfo GetTestProjectDirectory()
    {
        var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Assert.NotNull(currentDir);
        var dir = new DirectoryInfo(currentDir);
        var projFile = "Altinn.App.Analyzers.Tests.csproj";
        for (int i = 0; i < 10 && dir is not null && !dir.GetFiles(projFile).Any(); i++)
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("Could not find test project directory");
        return dir;
    }
}
