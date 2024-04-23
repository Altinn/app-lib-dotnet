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
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        if (instanceGuid is not null)
            activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        return activity;
    }

    internal Activity? StartQueryInstancesActivity(InstanceType type, string token)
    {
        var activity = ActivitySource.StartActivity(TraceNameQuery);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag("token", token); // TODO: magic string -> boo!
        return activity;
    }

    internal Activity? StartCreateInstanceActivity(InstanceType type)
    {
        var activity = ActivitySource.StartActivity(TraceNameCreate);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        return activity;
    }

    internal Activity? StartDeleteInstanceActivity(InstanceType type, Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameCreate);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateProcessActivity(InstanceType type)
    {
        var activity = ActivitySource.StartActivity(TraceNameProcess);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        return activity;
    }

    internal Activity? StartCompleteConfirmationActivity(InstanceType type, Guid instanceGuid)
    {
        var activity = ActivitySource.StartActivity(TraceNameConfirmation);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        return activity;
    }

    internal Activity? StartUpdateReadStatusActivity(InstanceType type, Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameReadStatus);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateSubStatusActivity(InstanceType type, Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameSubStatus);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdatePresentationTextActivity(InstanceType type, Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNamePresentationText);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
        return activity;
    }

    internal Activity? StartUpdateDataValuesActivity(InstanceType type, Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity(TraceNameDataValues);
        activity?.SetTag(InstanceLabels.Type, type.ToStringFast());
        activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId);
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
            [Display(Name = "create_instance")]
            CreateInstance,
            [Display(Name = "delete_instance")]
            DeleteInstance,
            [Display(Name = "get_instances")]
            GetInstances,
            [Display(Name = "query_instances")]
            QueryInstances,
            [Display(Name = "update_process")]
            UpdateProcess,
            [Display(Name = "complete_confirmation")]
            CompleteConfirmation,
            [Display(Name = "update_read_status")]
            UpdateReadStatus,
            [Display(Name = "update_sub_status")]
            UpdateSubStatus,
            [Display(Name = "update_presentation_text")]
            UpdatePresentationText,
            [Display(Name = "update_data_values")]
            UpdateDataValues,
        }

        public static class InstanceLabels
        {
            public const string Type = "instance.type";
        }

    }
}