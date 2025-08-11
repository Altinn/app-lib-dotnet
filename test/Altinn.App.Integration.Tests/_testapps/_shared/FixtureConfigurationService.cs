using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

#nullable enable

namespace TestApp.Shared;

/// <summary>
/// Service that manages fixture configuration including dynamic updates.
/// Watches for config file changes and updates application state accordingly.
/// </summary>
internal sealed class FixtureConfigurationService : IDisposable
{
    private readonly string _configFile = "/App/fixture-config/config.json";
    private readonly string _configDir;
    private readonly string _configFileName;
    private FileSystemWatcher? _watcher;
    private readonly object _lock = new();
    public FixtureConfiguration? Config { get; private set; }

    public static FixtureConfigurationService Instance { get; } = new();

    // Event fired when configuration changes
    public event Action? ConfigurationChanged;

    private FixtureConfigurationService()
    {
        _configDir = Path.GetDirectoryName(_configFile)!;
        _configFileName = Path.GetFileName(_configFile);
    }

    /// <summary>
    /// Waits for initial configuration and sets up dynamic watching
    /// </summary>
    public void Initialize(TimeSpan? timeout = null)
    {
        // The AppFixture injects configuration including port and fixture instance
        // as soon as the app is in the `Starting` state.
        // This replaces the old port configuration system with a more comprehensive approach.
        timeout ??= TimeSpan.FromSeconds(10);

        using var resetEvent = new ManualResetEventSlim(false);

        // Set up watcher before checking file existence to avoid race condition
        SetupWatcher(resetEvent);

        // Check if file already exists
        if (File.Exists(_configFile))
        {
            LoadConfiguration();
            return;
        }

        if (!resetEvent.Wait(timeout.Value))
            throw new TimeoutException(
                $"Fixture configuration not available after {timeout.Value.TotalSeconds} seconds"
            );

        LoadConfiguration();
    }

    public void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // Check for scenario-specific services
        // Through "_scenarios" we can override/inject both configuration
        // and code that is specific to a test scenario.
        // This allows us to run the same app container image with slightly different
        // configurations and code which is more efficient than having to create and build a whole other app/image.
        var config = Config ?? throw new InvalidOperationException("Fixture configuration not initialized");

        services.AddTracingServices();

        var scenario = config.AppScenario ?? "default";
        if (scenario != "default")
        {
            var scenarioOverridePath = Path.Combine(env.ContentRootPath, "scenario-overrides", "services");
            if (Directory.Exists(scenarioOverridePath))
            {
                try
                {
                    var csFiles = Directory.GetFiles(scenarioOverridePath, "*.cs", SearchOption.AllDirectories);
                    if (!csFiles.Any())
                        return;

                    var compiledAssembly = CompileScenarioServices(csFiles);
                    if (compiledAssembly is not null)
                    {
                        var serviceCount = RegisterServicesFromAssembly(services, compiledAssembly);
                    }
                    else
                    {
                        SnapshotLogger.LogInitError("Failed to compile scenario services assembly");
                    }
                }
                catch (Exception ex)
                {
                    SnapshotLogger.LogInitError($"Failed to register scenario services: {ex.Message}");
                }
            }
            else
            {
                SnapshotLogger.LogInitError(
                    $"Scenario '{scenario}' specified but no services directory found at {scenarioOverridePath}"
                );
            }
        }
    }

    private void SetupWatcher(ManualResetEventSlim? initialResetEvent = null)
    {
        if (_watcher != null)
            return;

        lock (_lock)
        {
            if (_watcher != null)
                return;

            try
            {
                _watcher = new FileSystemWatcher(_configDir, _configFileName);
                _watcher.Created += (sender, e) =>
                {
                    initialResetEvent?.Set();
                    LoadConfiguration();
                };
                _watcher.Changed += (sender, e) =>
                {
                    initialResetEvent?.Set();
                    LoadConfiguration();
                };
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                SnapshotLogger.LogInitError($"Failed to setup config file watcher: {ex.Message}");
            }
        }
    }

    private void LoadConfiguration()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_configFile))
                    return;

                var configJson = File.ReadAllText(_configFile);
                var config = JsonSerializer.Deserialize<FixtureConfiguration>(configJson);
                if (config is null)
                {
                    SnapshotLogger.LogInitError("Failed to deserialize fixture configuration.");
                    return;
                }

                if (config != Config)
                {
                    Config = config;
                    SyncScenarioConfig();
                    Environment.SetEnvironmentVariable(
                        "GeneralSettings__ExternalAppBaseUrl",
                        config.ExternalAppBaseUrl
                    );
                    ConfigurationChanged?.Invoke();
                    SnapshotLogger.LogInitInfo($"Fixture configuration updated");
                }
            }
            catch (Exception ex)
            {
                SnapshotLogger.LogInitError($"Failed to read fixture configuration: {ex.Message}");
            }
        }
    }

    private static void SyncScenarioConfig()
    {
        var scenarioConfigPath = "/App/scenario-overrides/config";
        if (!Directory.Exists(scenarioConfigPath))
        {
            SnapshotLogger.LogInitWarning($"No scenario config directory found at {scenarioConfigPath}");
            return;
        }
        var targetConfigPath = "/App/config";

        foreach (var file in Directory.GetFiles(scenarioConfigPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(scenarioConfigPath, file);
            var targetFile = Path.Combine(targetConfigPath, relativePath);
            var targetDir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(file, targetFile, overwrite: true);
        }
    }

    private Assembly? CompileScenarioServices(string[] csFiles)
    {
        var sourceTexts = csFiles.Select(file => File.ReadAllText(file)).ToList();
        if (!sourceTexts.Any())
            return null;

        var references = DependencyContext
            .Default.CompileLibraries.SelectMany(cl => cl.ResolveReferencePaths())
            .Select(asm => MetadataReference.CreateFromFile(asm))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            assemblyName: $"ScenarioServices_{Guid.NewGuid():N}",
            syntaxTrees: sourceTexts.Select(source => CSharpSyntaxTree.ParseText(source)),
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        if (!emitResult.Success)
        {
            var errors = string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString()));
            SnapshotLogger.LogInitError($"Compilation failed:\n{errors}");
            return null;
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        return AssemblyLoadContext.Default.LoadFromStream(memoryStream);
    }

    private static int RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        int registeredCount = 0;
        foreach (var type in assembly.GetTypes())
        {
            var method = type.GetMethod("RegisterServices", BindingFlags.Public | BindingFlags.Static);
            if (
                method != null
                && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType == typeof(IServiceCollection)
            )
            {
                method.Invoke(null, new object[] { services });
                registeredCount++;
            }
        }
        return registeredCount;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}
