using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.ApplicationMetadata;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetApplicationMetadataActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.Get");
        return activity;
    }

    internal Activity? StartGetApplicationXACMLPolicyActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetXACMLPolicy");
        return activity;
    }

    internal Activity? StartGetApplicationBPMNProcessActivity()
    {
        var activity = ActivitySource.StartActivity($"{_prefix}.GetBPMNProcess");
        return activity;
    }

    internal static class ApplicationMetadata
    {
        internal const string _prefix = "ApplicationMetadata";
    }
}
