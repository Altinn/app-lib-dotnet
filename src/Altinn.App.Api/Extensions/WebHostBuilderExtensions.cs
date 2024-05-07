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

                var (appInsightsKey, appInsightsConnectionString) = ServiceCollectionExtensions.GetAppInsightsConfig(
                    config,
                    env
                );
                if (
                    string.IsNullOrWhiteSpace(appInsightsConnectionString) && !string.IsNullOrWhiteSpace(appInsightsKey)
                )
                {
                    appInsightsConnectionString = $"InstrumentationKey={appInsightsKey}";
                }

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
                    var service = services.LastOrDefault(s => s.ServiceType == typeof(AltinnAppBuilder));
                    if (service is null)
                    {
                        throw new InvalidOperationException(
                            "AltinnAppBuilder not registered yet. Make sure AddAltinnAppServices is called earlier in the pipeline"
                        );
                    }
                    var appBuilder = service.ImplementationInstance as AltinnAppBuilder;
                    Debug.Assert(
                        appBuilder is not null,
                        "If the AltinnAppBuilder service registration is found, the instance should be here"
                    );

                    if (appBuilder.LoggingConfigurator is not null)
                    {
                        appBuilder.LoggingConfigurator(options);
                    }
                });
            });
        });
    }
}
