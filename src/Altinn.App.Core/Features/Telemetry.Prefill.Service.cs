using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.Data;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartPrefillDataModelActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.PrefillDataModel");
        return activity;
    }

    internal Activity? StartPrefillDataModelActivity(string? partyId)
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.PrefillDataModelWithId");
        activity.SetInstanceOwnerPartyId(partyId);
        return activity;
    }

    internal static class PrefillService
    {
        internal const string _prefix = "PrefillService";
    }
}
