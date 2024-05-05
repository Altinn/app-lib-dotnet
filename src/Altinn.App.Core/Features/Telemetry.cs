using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features;

/// <summary>
/// Used for creating traces and metrics for the app.
/// </summary>
public sealed partial class Telemetry : IDisposable
{
    private bool _disposed;
    private bool _isInitialized;
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

    private FrozenDictionary<string, Counter<long>> _counters;
    private FrozenDictionary<string, Histogram<double>> _histograms;

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

        _counters = FrozenDictionary<string, Counter<long>>.Empty;
        _histograms = FrozenDictionary<string, Histogram<double>>.Empty;

        Init();
    }

    internal void Init()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;
            _isInitialized = true;

            var counters = new Dictionary<string, Counter<long>>();
            var histograms = new Dictionary<string, Histogram<double>>();
            var context = new InitContext(counters, histograms);

            InitData(context);
            InitInstances(context);
            InitNotifications(context);
            InitProcesses(context);
            InitValidation(context);

            _counters = counters.ToFrozenDictionary();
            _histograms = histograms.ToFrozenDictionary();
        }
    }

    private readonly record struct InitContext(
        Dictionary<string, Counter<long>> Counters,
        Dictionary<string, Histogram<double>> Histograms
    );

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
    /// Labels used to tag traces for observability.
    /// </summary>
    public static class Labels
    {
        /// <summary>
        /// Label for the party ID of the instance owner.
        /// </summary>
        public static readonly string InstanceOwnerPartyId = "instance.owner_party_id";

        /// <summary>
        /// Label for the guid that identifies the instance.
        /// </summary>
        public static readonly string InstanceGuid = "instance.guid";

        /// <summary>
        /// Label for the guid that identifies the data.
        /// </summary>
        public static readonly string DataGuid = "data.guid";

        /// <summary>
        /// Label for the ID of the task.
        /// </summary>
        public static readonly string TaskId = "task.id";
    }

    private void InitMetricCounter(InitContext context, string name, Action<Counter<long>> init)
    {
        var counter = Meter.CreateCounter<long>(name, unit: null, description: null);
        context.Counters.Add(name, counter);
        init(counter);
    }

    private void InitMetricHistogram(InitContext context, string name)
    {
        var histogram = Meter.CreateHistogram<double>(name, unit: null, description: null);
        context.Histograms.Add(name, histogram);
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
