using Altinn.App.Core.Extensions;

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
            (_, configBuilder) =>
            {
                var config = new List<KeyValuePair<string, string?>>();

                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (string.IsNullOrWhiteSpace(environment))
                    environment = Environments.Production;

                var isDevelopment = environment == Environments.Development;
                if (isDevelopment)
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
}
