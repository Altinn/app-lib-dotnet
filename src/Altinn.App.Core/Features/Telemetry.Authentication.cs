using System.Diagnostics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartRefreshAuthenticationTokenActivity()
    {
        var activity = ActivitySource.StartActivity($"Authentication.Refresh");
        return activity;
    }
}
