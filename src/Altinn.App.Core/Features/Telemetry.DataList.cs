using System.Diagnostics;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartDataListActivity()
    {
        return ActivitySource.StartActivity("DataList.GetAsync");
    }

    internal Activity? StartDataListActivity(InstanceIdentifier instanceIdentifier)
    {
        var activity = ActivitySource.StartActivity("DataList.GetAsyncWithId");
        activity.SetInstanceId(instanceIdentifier.InstanceGuid);
        return activity;
    }
}
