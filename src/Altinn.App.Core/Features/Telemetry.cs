using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace Altinn.App.Core.Features;

internal static class TelemetryExtensions
{
    internal static void Errored(this Activity? activity, Exception? exception = null, string? error = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, error);
        activity?.RecordException(exception);
    }
}

/// <summary>
/// Used for creating traces and metrics for the app.
/// </summary>
public sealed partial class Telemetry : IDisposable
{
    private bool _disposed;
    private readonly object _lock = new();

    // /// <summary>
    // /// Object for managing counters for the app.
    // /// </summary>
    // public readonly CountersRegistry Counters;

    /// <summary>
    /// Gets the ActivitySource for the app.
    /// Using this, you can create traces that are transported to the OpenTelemetry collector.
    /// </summary>
    public ActivitySource ActivitySource { get; }

    /// <summary>
    /// Gets the Meter for the app.
    /// Using this, you can create metrics that are transported to the OpenTelemetry collector.
    /// </summary>
    public Meter Meter { get; }

    private readonly Dictionary<string, Counter<long>> _counters = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Telemetry"/> class.
    /// </summary>
    /// <param name="appIdentifier"></param>
    /// <param name="appSettings"></param>
    public Telemetry(AppIdentifier appIdentifier, IOptions<AppSettings> appSettings)
    {
        var appId = appIdentifier.App;
        var appVersion = appSettings.Value.AppVersion;
        ActivitySource = new ActivitySource(appId, appVersion);
        Meter = new Meter(appId, appVersion);
        // Counters = new(this);

        InitNotifications();
    }

    // public sealed class CountersRegistry
    // {
    //     private readonly Telemetry _parent;

    //     internal CountersRegistry(Telemetry telemetry) => _parent = telemetry;

    //     public void Increment(string name, ulong delta = 1)
    //     {
    //         name = Metrics.CreateName(name);
    //         var counter = _parent.GetCounter(name, static (name, self) => self.Meter.CreateCounter<long>(name));
    //         counter.Add((long)delta);
    //     }
    // }

    /// <summary>
    /// Utility methods for creating metrics for an app.
    /// </summary>
    public static class Metrics
    {
        internal static readonly string Prefix = "altinn_app_lib";
        internal static readonly string PrefixCustom = "altinn_app";

        /// <summary>
        /// Creates a name for a metric with the prefix "altinn_app".
        /// </summary>
        /// <param name="name">Name of the metric, naming-convention is 'snake_case'</param>
        /// <returns>Full metric name</returns>
        public static string CreateName(string name) => $"{PrefixCustom}_{name}";

        internal static string CreateLibName(string name) => $"{Prefix}_{name}";
    }

    /// <summary>
    /// Disposes the Telemetry object.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            ActivitySource?.Dispose();
            Meter?.Dispose();
        }
    }
}
