using System.Diagnostics;
using NetEscapades.EnumGenerators;
using System.ComponentModel.DataAnnotations;
using static Altinn.App.Core.Features.Telemetry.Instance;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetInstanceActivity(InstanceType type, Guid? instanceGuid = null)
    {
        var activity = ActivitySource.StartActivity(TraceNameGet);
        if(activity is not null)
        {
            activity.SetTag(InstanceLabels.Type, type.ToStringFast());
            if (instanceGuid is not null)
                activity.SetTag(Labels.InstanceGuid, instanceGuid);
        }
        return activity;
    }

    internal Activity? StartQueryInstancesActivity(string token)
    {
        var activity = ActivitySource.StartActivity(TraceNameQuery);
        if(activity is not null)
        {
            activity.SetTag("token", token); // TODO: magic string -> boo!
        }
        return activity;
    }

    internal Activity? StartCreateInstanceActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameCreate);
        return activity;
    }

    internal Activity? StartDeleteInstanceActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameCreate);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal Activity? StartUpdateProcessActivity(Platform.Storage.Interface.Models.Instance instance)
    {
        var activity = ActivitySource.StartActivity(TraceNameProcess);
        if(activity is not null)
        {
            Guid InstanceGuid = Guid.Parse(instance.Id.Split('/')[1]);
            activity.SetTag(Labels.InstanceGuid, InstanceGuid);
        }
        return activity;
    }

    internal Activity? StartCompleteConfirmationActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameConfirmation);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal Activity? StartUpdateReadStatusActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameReadStatus);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal Activity? StartUpdateSubStatusActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameSubStatus);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal Activity? StartUpdatePresentationTextActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNamePresentationText);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal Activity? StartUpdateDataValuesActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameDataValues);
        if(activity is not null)
        {
            activity.SetTag(Labels.InstanceGuid, instanceGuid);
            activity.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        }
        return activity;
    }

    internal static class Instance
    {
        private const string _prefix = "Instance";

        internal const string TraceNameGet = $"{_prefix}.Get";
        internal const string TraceNameQuery = $"{_prefix}.Query";
        internal const string TraceNameCreate = $"{_prefix}.Create";
        internal const string TaceNameDelete = $"{_prefix}.Delete";
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