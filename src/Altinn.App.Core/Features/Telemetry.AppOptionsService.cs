using System.Diagnostics;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetOptionsAsyncActivity()
    {
        return ActivitySource.StartActivity("AppOptionsService.GetOptionsAsync");
    }

    internal Activity? StartGetOptionsAsyncActivity(InstanceIdentifier instanceIdentifier)
    {
        var activity = ActivitySource.StartActivity("AppOptionsService.GetOptionsAsync");
        activity.SetInstanceId(instanceIdentifier.InstanceGuid);
        return activity;
    }
}
