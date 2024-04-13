using System.Diagnostics.Metrics;

namespace Altinn.App.Api.Infrastructure.Telemetry;

/// <summary>
/// Provides a factory interface for creating metric meters.
/// </summary>
public interface IMetricFactory
{
    /// <summary>
    /// Creates a new meter instance using configuration settings obtained from environment variables.
    /// </summary>
    /// <returns>A new instance of a meter.</returns>
    Meter Create();
}

/// <summary>
/// Factory creating .Net Meters
/// </summary>
public class MetricFactory : IMetricFactory
{
    private readonly IMeterFactory _meterFactory;

    /// <summary>
    /// Initializes a new instance of the MetricFactory class using the specified IMeterFactory.
    /// </summary>
    /// <param name="meterFactory">The factory to create Meter instances.</param>
    public MetricFactory(IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory ?? throw new ArgumentNullException(nameof(meterFactory));
    }
    
    /// <summary>
    /// Creates a new meter instance using configuration settings obtained from environment variables.
    /// </summary>
    /// <returns>A new instance of a meter.</returns>
    public Meter Create()
    {
        // TODO: use actual env vars
        string meterName = Environment.GetEnvironmentVariable("METER_NAME") ?? "DefaultMeterName";
        string meterVersion = Environment.GetEnvironmentVariable("METER_VERSION") ?? "1.0";

        var op = new MeterOptions(meterName)
        {
            Version = meterVersion
        };

        return _meterFactory.Create(op);
    }
}