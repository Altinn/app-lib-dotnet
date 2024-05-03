using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using NetEscapades.EnumGenerators;
using static Altinn.App.Core.Features.Telemetry.Data;
using Tag = System.Collections.Generic.KeyValuePair<string, object?>;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    private void InitData()
    {
        InitMetricCounter(
            MetricNameDataPatched,
            init: static m =>
            {
                m.Add(0, new Tag(ResultLabel, PatchResult.Success.ToStringFast()));
                m.Add(0, new Tag(ResultLabel, PatchResult.Error.ToStringFast()));
            }
        );
    }

    internal void DataPatched(PatchResult result) =>
        _counters[MetricNameDataPatched].Add(1, new Tag(ResultLabel, result.ToStringFast()));

    internal Activity? StartDataPatchActivity(Instance instance)
    {
        var activity = ActivitySource.StartActivity(TraceNamePatch);
        activity.SetInstanceId(instance);
        return activity;
    }

    internal static class Data
    {
        internal static readonly string ResultLabel = "result";

        private const string _prefix = "Data";

        internal static readonly string MetricNameDataPatched = Metrics.CreateLibName("datum_patched");

        internal const string TraceNamePatch = $"{_prefix}.Patch";

        [EnumExtensions]
        internal enum PatchResult
        {
            [Display(Name = "success")]
            Success,

            [Display(Name = "error")]
            Error
        }
    }
}
