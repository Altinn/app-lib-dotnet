using System.Diagnostics;
using static Altinn.App.Core.Features.Telemetry.ApplicationMetadata;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartGetApplicationMetadataActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameGet);
        return activity;
    }

    internal Activity? StartGetApplicationXACMLPolicyActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameGetXACMLPolicy);
        return activity;
    }

    internal Activity? StartGetApplicationBPMNProcessActivity()
    {
        var activity = ActivitySource.StartActivity(TraceNameGetBPMNProcess);
        return activity;
    }

    internal static class ApplicationMetadata
    {
        private const string _prefix = "ApplicationMetadata";

        internal const string TraceNameGet = $"{_prefix}.Get";
        internal const string TraceNameGetXACMLPolicy = $"{_prefix}.GetXACMLPolicy";
        internal const string TraceNameGetBPMNProcess = $"{_prefix}.GetBPMNProcess";
    }
}
