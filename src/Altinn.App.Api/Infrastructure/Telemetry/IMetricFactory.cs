using System.Diagnostics.Metrics;

namespace Altinn.App.Api.Infrastructure.Telemetry;

/// <summary>
/// Provides a factory interface for creating metric meters.
/// This interface extends the IMeterFactory to simplyfy creation
/// </summary>
public interface IMetricFactory : IMeterFactory
{
    /// <summary>
    /// Creates a new meter instance using configuration settings obtained from environment variables.
    /// </summary>
    /// <returns>A new instance of a meter.</returns>
    Meter Create();
}

/// <summary>
/// s
/// </summary>
public class MetricFactory : IMetricFactory
{
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

        return Create(op);
    }

    /// <summary>
    /// Creates a Meter from MeterOptions.
    /// </summary>
    /// <param name="options">Meter options.</param>
    /// <returns></returns>
    public Meter Create(MeterOptions options)
    {
        return new Meter(options);
    }

    /// <summary>
    /// Implemented to satisfy the IDisposable interface requirement of IMeterFactory
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose, but implemented to satisfy the IDisposable interface requirement of IMeterFactory
    }
}