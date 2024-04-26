using System.Diagnostics;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.Processes;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    private void InitProcesses()
    {
        _counters.Add(
            MetricNameProcessesCreated,
            Meter.CreateCounter<long>(MetricNameProcessesCreated, unit: null, description: null)
        );
        _counters.Add(
            MetricNameProcessesEnded,
            Meter.CreateCounter<long>(MetricNameProcessesEnded, unit: null, description: null)
        );

        _histograms.Add(
            MetricNameProcessesDuration,
            Meter.CreateHistogram<double>(MetricNameProcessesDuration, unit: null, description: null)
        );
    }

    internal void ProcessStarted()
    {
        _counters[MetricNameProcessesCreated].Add(1);
    }

    internal void ProcessEnded(ProcessStateChange processChange)
    {
        ArgumentNullException.ThrowIfNull(processChange?.NewProcessState?.Started);
        ArgumentNullException.ThrowIfNull(processChange?.NewProcessState?.Ended);
        var state = processChange.NewProcessState;

        _counters[MetricNameProcessesEnded].Add(1);
        var duration = state.Ended.Value - state.Started.Value;
        _histograms[MetricNameProcessesDuration].Record(duration.TotalSeconds);
    }

    internal Activity? StartProcessStartActivity(Instance instance)
    {
        var activity = ActivitySource.StartActivity(TraceNameStart);
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
        }
        return activity;
    }

    internal Activity? StartProcessNextActivity(Instance instance)
    {
        var activity = ActivitySource.StartActivity(TraceNameNext);
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
        }
        return activity;
    }

    internal Activity? StartProcessEndActivity(Instance instance)
    {
        ArgumentNullException.ThrowIfNull(instance?.Process);

        var activity = ActivitySource.StartActivity(TraceNameEnd);
        if (activity is not null)
        {
            TryAddInstanceId(activity, instance);
        }
        return activity;
    }

    internal static class Processes
    {
        private const string _prefix = "Process";

        internal static readonly string MetricNameProcessesCreated = Metrics.CreateLibName("processes_created");
        internal static readonly string MetricNameProcessesEnded = Metrics.CreateLibName("processes_ended");
        internal static readonly string MetricNameProcessesDuration = Metrics.CreateLibName("processes_duration");

        internal const string TraceNameStart = $"{_prefix}.Start";
        internal const string TraceNameNext = $"{_prefix}.Next";
        internal const string TraceNameEnd = $"{_prefix}.End";
    }
}
