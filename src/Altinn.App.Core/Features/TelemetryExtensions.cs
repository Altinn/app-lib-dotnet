using System.Diagnostics;
using Altinn.Platform.Storage.Interface.Models;
using OpenTelemetry.Trace;
using Labels = Altinn.App.Core.Features.Telemetry.Labels;

namespace Altinn.App.Core.Features;

public static class TelemetryExtensions
{
    public static Activity? SetInstanceId(this Activity? activity, Instance? instance)
    {
        if (instance?.Id is not null)
        {
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        }
        return activity;
    }

    public static Activity? SetInstanceId(this Activity? activity, string? instanceId)
    {
        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            Guid instanceGuid = Guid.Parse(instanceId.Split("/")[1]);
            activity?.SetTag(Labels.InstanceGuid, instanceGuid);
        }
        return activity;
    }

    public static Activity? SetInstanceId(this Activity? activity, Guid? instanceGuid)
    {
        if (instanceGuid is not null)
        {
            activity?.SetTag(Labels.InstanceGuid, instanceGuid.Value);
        }

        return activity;
    }

    public static Activity? SetInstanceOwnerPartyId(this Activity? activity, int? instanceOwnerPartyId)
    {
        if (instanceOwnerPartyId is not null)
        {
            activity?.SetTag(Labels.InstanceOwnerPartyId, instanceOwnerPartyId.Value);
        }

        return activity;
    }

    public static Activity? SetDataElementId(this Activity? activity, DataElement? dataElement)
    {
        if (dataElement?.Id is not null)
        {
            Guid dataGuid = Guid.Parse(dataElement.Id);
            activity?.SetTag(Labels.DataGuid, dataGuid);
        }

        return activity;
    }

    public static Activity? SetDataElementId(this Activity? activity, string? dataElementId)
    {
        if (!string.IsNullOrWhiteSpace(dataElementId))
        {
            Guid dataGuid = Guid.Parse(dataElementId);
            activity?.SetTag(Labels.DataGuid, dataGuid);
        }

        return activity;
    }

    public static Activity? SetTaskId(this Activity? activity, string? taskId)
    {
        if (!string.IsNullOrWhiteSpace(taskId))
        {
            activity?.SetTag(Labels.TaskId, taskId);
        }

        return activity;
    }

    internal static void Errored(this Activity? activity, Exception? exception = null, string? error = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, error);
        activity?.RecordException(exception);
    }
}
