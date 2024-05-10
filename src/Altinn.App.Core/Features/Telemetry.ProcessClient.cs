using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartGetProcessDefinitionActivity() =>
        ActivitySource.StartActivity("PdfService.GetProcessDefinition");

    internal Activity? StartGetProcessHistoryActivity(string? instanceId, string? instanceOwnerPartyId)
    {
        var activity = ActivitySource.StartActivity("PdfService.GetProcessHistory");
        activity.SetInstanceId(instanceId);
        activity.SetInstanceOwnerPartyId(instanceOwnerPartyId);
        return activity;
    }
}
