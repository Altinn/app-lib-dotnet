using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartAqzActivity() => ActivitySource.StartActivity("ProcessClient.GetProcessDefinition");

    internal Activity? StartAcquireProcessLockActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity("AcquireProcessLock");
        activity?.SetInstanceId(instanceGuid);
        activity?.SetInstanceOwnerPartyId(instanceOwnerPartyId);

        return activity;
    }

    internal Activity? StartReleaseProcessLockActivity(Guid instanceGuid, int instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity("ReleaseProcessLock");
        activity?.SetInstanceId(instanceGuid);
        activity?.SetInstanceOwnerPartyId(instanceOwnerPartyId);

        return activity;
    }
}
