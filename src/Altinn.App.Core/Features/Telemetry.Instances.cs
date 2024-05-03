using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using NetEscapades.EnumGenerators;
using static Altinn.App.Core.Features.Telemetry.Instances;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    private void InitInstances()
    {
        InitMetricCounter(MetricNameInstancesCreated, init: static m => m.Add(0));
        InitMetricCounter(MetricNameInstancesCompleted, init: static m => m.Add(0));
        InitMetricCounter(MetricNameInstancesDeleted, init: static m => m.Add(0));

        InitMetricHistogram(MetricNameInstancesDuration);
    }

    internal void InstanceCreated(Instance instance) => _counters[MetricNameInstancesCreated].Add(1);

    internal void InstanceCompleted(Instance instance)
    {
        _counters[MetricNameInstancesCompleted].Add(1);

        if (instance.Created is not null)
        {
            var duration = DateTime.UtcNow - instance.Created.Value;
            _histograms[MetricNameInstancesDuration].Record(duration.TotalSeconds);
        }
    }

    internal void InstanceDeleted(Instance instance)
    {
        _counters[MetricNameInstancesDeleted].Add(1);

        if (instance.Created is not null)
        {
            var duration = DateTime.UtcNow - instance.Created.Value;
            _histograms[MetricNameInstancesDuration].Record(duration.TotalSeconds);
        }
    }

    internal Activity? StartGetInstanceActivity(InstanceType type, Guid? instanceGuid = null)
    {
        var activity = ActivitySource.StartActivity(TraceNameGet);
        if (activity is not null)
        {
            activity.SetTag(InstanceLabels.Type, type.ToStringFast());
            activity.SetInstanceId(instanceGuid);
        }
        return activity;
    }

    internal Activity? StartQueryInstancesActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameQuery);
        return activity;
    }

    internal Activity? StartCreateInstanceActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameCreate);
        return activity;
    }

    internal Activity? StartDeleteInstanceActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameDelete);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateProcessActivity(Instance instance)
    {
        var activity = ActivitySource.StartActivity(TraceNameProcess);
        activity.SetInstanceId(instance);
        return activity;
    }

    internal Activity? StartCompleteConfirmationActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameConfirmation);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateReadStatusActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameReadStatus);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateSubStatusActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameSubStatus);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdatePresentationTextActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNamePresentationText);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateDataValuesActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameDataValues);
        activity.SetInstanceId(instanceGuid);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }

    internal static class Instances
    {
        private const string _prefix = "Instance";

        internal static readonly string MetricNameInstancesCreated = Metrics.CreateLibName("instances_created");
        internal static readonly string MetricNameInstancesCompleted = Metrics.CreateLibName("instances_completed");
        internal static readonly string MetricNameInstancesDeleted = Metrics.CreateLibName("instances_deleted");
        internal static readonly string MetricNameInstancesDuration = Metrics.CreateLibName("instances_duration");

        internal const string TraceNameGet = $"{_prefix}.Get";
        internal const string TraceNameQuery = $"{_prefix}.Query";
        internal const string TraceNameCreate = $"{_prefix}.Create";
        internal const string TraceNameDelete = $"{_prefix}.Delete";
        internal const string TraceNameProcess = $"{_prefix}.UpdateProcess";
        internal const string TraceNameConfirmation = $"{_prefix}.CompleteConfirmation";
        internal const string TraceNameReadStatus = $"{_prefix}.UpdateReadStatus";
        internal const string TraceNameSubStatus = $"{_prefix}.UpdateSubStatus";
        internal const string TraceNamePresentationText = $"{_prefix}.UpdatePresentationText";
        internal const string TraceNameDataValues = $"{_prefix}.UpdateDataValues";

        [EnumExtensions]
        internal enum InstanceType
        {
            [Display(Name = "get_instance_by_guid")]
            GetInstanceByGuid,

            [Display(Name = "get_instance_by_instance")]
            GetInstanceByInstance,

            [Display(Name = "get_instances")]
            GetInstances,
        }

        internal static class InstanceLabels
        {
            internal const string Type = "instance.get.type";
        }
    }
}
