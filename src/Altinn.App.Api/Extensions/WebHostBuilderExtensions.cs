using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.App;
using AltinnCore.Authentication.Constants;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

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
                configBuilder.LoadAppConfig(args);
            }
        );
    }

    /// <summary>
    /// Add KeyVault as a configuration provider. Requires that the kvSetting section is present in the configuration and throws an exception if not. See documentation for secret handling in Altinn apps.
    /// </summary>
    /// <param name="builder"></param>
    public static void AddKeyVaultAsConfigProvider(this IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (_, configBuilder) =>
            {
                IConfiguration stageOneConfig = configBuilder.Build();
                var keyVaultSettings = new KeyVaultSettings();
                stageOneConfig.GetSection("kvSetting").Bind(keyVaultSettings);

                if (
                    string.IsNullOrEmpty(keyVaultSettings.ClientId)
                    || string.IsNullOrEmpty(keyVaultSettings.TenantId)
                    || string.IsNullOrEmpty(keyVaultSettings.ClientSecret)
                    || string.IsNullOrEmpty(keyVaultSettings.SecretUri)
                )
                {
                    throw new ApplicationConfigException(
                        "Attempted to add KeyVault as a configuration provider, but the required settings for authenticating with KeyVault are missing. Please check the configuration."
                    );
                }

                var clientSecretCredential = new ClientSecretCredential(
                    keyVaultSettings.TenantId,
                    keyVaultSettings.ClientId,
                    keyVaultSettings.ClientSecret
                );

                var secretClient = new SecretClient(new Uri(keyVaultSettings.SecretUri), clientSecretCredential);

                configBuilder.AddAzureKeyVault(
                    secretClient,
                    new Azure.Extensions.AspNetCore.Configuration.Secrets.KeyVaultSecretManager()
                );
            }
        );
    }
}
