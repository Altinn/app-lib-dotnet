using System.Diagnostics.CodeAnalysis;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Altinn.App.Api.Extensions;

/// <summary>
/// Class for defining extensions to IWebHostBuilder for AltinnApps
/// </summary>
public sealed class AltinnAppBuilder : IDisposable
{
    private IOpenTelemetryBuilder? _openTelemetryBuilder;

    /// <summary>
    /// Logging configurator
    /// </summary>
    internal Action<OpenTelemetryLoggerOptions>? LoggingConfigurator { get; private set; }

    /// <summary>
    /// Builder
    /// </summary>
    public IOpenTelemetryBuilder OpenTelemetry =>
        _openTelemetryBuilder
        ?? throw new InvalidOperationException(
            "OpenTelemetry is not enabled - set 'UseOpenTelemetry' to 'true' in appsettings to use OpenTelemetry"
        );

    /// <summary>
    /// Experimental API for configuring OpenTelemetry logging.
    /// In newer versions of OpenTelemetry, this API will be improved
    /// </summary>
    /// <param name="configure">configuration delegate</param>
    /// <returns></returns>
    [Experimental("ALTINN010")]
    public AltinnAppBuilder ConfigureOpenTelemetryLogging(Action<OpenTelemetryLoggerOptions> configure)
    {
        LoggingConfigurator = configure;
        return this;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="openTelemetryBuilder">Builder</param>
    internal AltinnAppBuilder(IOpenTelemetryBuilder? openTelemetryBuilder)
    {
        _openTelemetryBuilder = openTelemetryBuilder;
        LoggingConfigurator = null;
    }

    /// <summary>
    /// Disposes the app builder, handled by DI container
    /// </summary>
    public void Dispose()
    {
        LoggingConfigurator = null;
        _openTelemetryBuilder = null;
    }
}
