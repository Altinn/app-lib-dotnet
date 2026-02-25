using Altinn.App.Core.Configuration;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace Altinn.App.Api.Extensions;

/// <summary>
/// Class for defining extensions to IWebHostBuilder for AltinnApps
/// </summary>
public static class WebHostBuilderExtensions
{
    /// <summary>
    /// Configure webhost with default values for Altinn Apps
    /// </summary>
    /// <param name="builder">The <see cref="IWebHostBuilder"/> being configured</param>
    /// <param name="args">Application arguments</param>
    public static void ConfigureAppWebHost(this IWebHostBuilder builder, string[] args)
    {
        builder.ConfigureAppConfiguration(
            (context, configBuilder) =>
            {
                var config = new List<KeyValuePair<string, string?>>();

                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.Add(new("OTEL_TRACES_SAMPLER", "always_on"));
                    config.Add(new("OTEL_METRIC_EXPORT_INTERVAL", "10000"));
                    config.Add(new("OTEL_METRIC_EXPORT_TIMEOUT", "8000"));
                }

                configBuilder.AddInMemoryCollection(config);

                var runtimeSecretsDirectory = context.Configuration["AppSettings:RuntimeSecretsDirectory"];
                if (string.IsNullOrWhiteSpace(runtimeSecretsDirectory))
                {
                    runtimeSecretsDirectory = AppSettings.DefaultRuntimeSecretsDirectory;
                }

                configBuilder.AddMaskinportenSettingsFile(context, runtimeSecretsDirectory);

                configBuilder.AddRuntimeConfigFiles(context.HostingEnvironment, runtimeSecretsDirectory);
                configBuilder.LoadAppConfig(args);
            }
        );
    }

    private static void AddRuntimeConfigFiles(
        this IConfigurationBuilder configBuilder,
        IHostEnvironment hostEnvironment,
        string secretsDirectory
    )
    {
        if (hostEnvironment.IsDevelopment())
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(secretsDirectory);
        const string overrideFileNameFragment = "override";
        if (!Directory.Exists(secretsDirectory))
        {
            return;
        }

        string[] jsonFiles = Directory.GetFiles(secretsDirectory, "*.json", SearchOption.TopDirectoryOnly);
        Array.Sort(jsonFiles, StringComparer.OrdinalIgnoreCase);

        PhysicalFileProvider? secretsFileProvider = null;
        HashSet<string> existingJsonFilePaths = [];

        foreach (JsonConfigurationSource source in configBuilder.Sources.OfType<JsonConfigurationSource>())
        {
            if (source.FileProvider is null || string.IsNullOrWhiteSpace(source.Path))
            {
                continue;
            }

            string? existingJsonFilePath = source.FileProvider.GetFileInfo(source.Path).PhysicalPath;
            if (string.IsNullOrWhiteSpace(existingJsonFilePath))
            {
                continue;
            }

            existingJsonFilePaths.Add(Path.GetFullPath(existingJsonFilePath));
        }

        foreach (string jsonFile in jsonFiles)
        {
            string jsonFilePath = Path.GetFullPath(jsonFile);
            if (existingJsonFilePaths.Contains(jsonFilePath))
            {
                continue;
            }

            string jsonFileName = Path.GetFileName(jsonFile);
            if (jsonFileName.Contains(overrideFileNameFragment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            configBuilder.AddJsonFile(
                provider: secretsFileProvider ??= new PhysicalFileProvider(secretsDirectory),
                path: jsonFileName,
                optional: true,
                reloadOnChange: true
            );
        }

        foreach (string jsonFile in jsonFiles)
        {
            string jsonFilePath = Path.GetFullPath(jsonFile);
            if (existingJsonFilePaths.Contains(jsonFilePath))
            {
                continue;
            }

            string jsonFileName = Path.GetFileName(jsonFile);
            if (!jsonFileName.Contains(overrideFileNameFragment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            configBuilder.AddJsonFile(
                provider: secretsFileProvider ??= new PhysicalFileProvider(secretsDirectory),
                path: jsonFileName,
                optional: true,
                reloadOnChange: true
            );
        }
    }

    private static IConfigurationBuilder AddMaskinportenSettingsFile(
        this IConfigurationBuilder configurationBuilder,
        WebHostBuilderContext context,
        string runtimeSecretsDirectory
    )
    {
        string? jsonProvidedPath = context.Configuration.GetValue<string>("MaskinportenSettingsFilepath");
        if (string.IsNullOrWhiteSpace(jsonProvidedPath))
        {
            jsonProvidedPath = Path.Join(runtimeSecretsDirectory, "maskinporten-settings.json");
        }
        string jsonAbsolutePath = Path.GetFullPath(jsonProvidedPath);

        if (File.Exists(jsonAbsolutePath))
        {
            string jsonDir = Path.GetDirectoryName(jsonAbsolutePath) ?? string.Empty;
            string jsonFile = Path.GetFileName(jsonAbsolutePath);

            configurationBuilder.AddJsonFile(
                provider: new PhysicalFileProvider(jsonDir),
                path: jsonFile,
                optional: true,
                reloadOnChange: true
            );
        }

        return configurationBuilder;
    }
}
