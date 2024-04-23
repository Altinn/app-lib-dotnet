using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Tests.Mocks;

internal sealed record TelemetryFake : IDisposable
{
    internal Telemetry Object { get; }

    internal ActivityListener ActivityListener { get; }

    internal MeterListener MeterListener { get; }

    private readonly ConcurrentBag<Activity> _activities = [];
    private readonly ConcurrentDictionary<string, IReadOnlyList<MetricMeasurement>> _metricValues = [];

    internal readonly record struct MetricMeasurement(long Value, IReadOnlyDictionary<string, object?> Tags);

    internal IEnumerable<Activity> CapturedActivities => _activities;

    internal IReadOnlyDictionary<string, IReadOnlyList<MetricMeasurement>> CapturedMetrics => _metricValues;

    internal TelemetryFake(string org = "ttd", string name = "test", string version = "v1")
    {
        var appId = new AppIdentifier(org, name);
        var options = new AppSettings
        {
            AppVersion = version,
        };

        ActivityListener = new ActivityListener()
        {
            ShouldListenTo = (activitySource) =>
            {
                return activitySource.Name == appId.App;
            },
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                _activities.Add(activity);
            },
        };
        ActivitySource.AddActivityListener(ActivityListener);

        MeterListener = new MeterListener()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != appId.App)
                {
                    return;
                }

                _metricValues.TryAdd(instrument.Name, new List<MetricMeasurement>());
                listener.EnableMeasurementEvents(instrument, this);
            },
        };
        MeterListener.SetMeasurementEventCallback<long>(static (instrument, measurement, tagSpan, state) =>
        {
            Debug.Assert(state is not null);
            var self = (TelemetryFake)state!;
            Debug.Assert(self._metricValues[instrument.Name] is List<MetricMeasurement>);
            var measurements = (List<MetricMeasurement>)self._metricValues[instrument.Name];
            var tags = new Dictionary<string, object?>(tagSpan.Length);
            for (int i = 0; i < tagSpan.Length; i++)
            {
                tags.Add(tagSpan[i].Key, tagSpan[i].Value);
            }

            foreach (var t in instrument.Tags ?? [])
            {
                tags.Add(t.Key, t.Value);
            }

            measurements.Add(new(measurement, tags));
        });
        MeterListener.Start();

        Object = new Telemetry(appId, Options.Create(options));
    }

    public void Dispose()
    {
        ActivityListener.Dispose();
        MeterListener.Dispose();
        Object.Dispose();
    }
}
