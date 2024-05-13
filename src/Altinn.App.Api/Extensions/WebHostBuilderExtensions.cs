using System.Diagnostics;
using Altinn.App.Core.Extensions;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Logs;

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
        IConfiguration? config = null;

        builder.ConfigureAppConfiguration(
            (_, configBuilder) =>
            {
                configBuilder.LoadAppConfig(args);
                config = configBuilder.Build();
            }
        );

        builder.ConfigureLogging(logging =>
        {
            if (config is null)
            {
                throw new InvalidOperationException("Can't configure logging without IConfiguration");
            }

            var useOpenTelemetrySetting = config.GetValue<bool?>("AppSettings:UseOpenTelemetry");
            if (useOpenTelemetrySetting is not true)
            {
                return;
            }

            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;

                var env =
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    ?? "Production";

                var appInsightsConnectionString = ServiceCollectionExtensions.GetAppInsightsConfigForOtel(config, env);

                if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                {
                    options.AddAzureMonitorLogExporter(options =>
                    {
                        options.ConnectionString = appInsightsConnectionString;
                    });
                }
                else
                {
                    options.AddOtlpExporter();
                }

                builder.ConfigureServices(services =>
                {
                    var appBuilder = services.GetInstanceInServices<AltinnAppBuilder>();

                    if (appBuilder.LoggingConfigurator is not null)
                    {
                        appBuilder.LoggingConfigurator(options);
                    }
                });
            });
        });
    }
}
