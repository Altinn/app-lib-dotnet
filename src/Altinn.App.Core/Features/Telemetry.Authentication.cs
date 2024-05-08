using System.Diagnostics;

namespace Altinn.App.Core.Features;

public partial class Telemetry
{
    internal Activity? StartRefreshAuthenticationTokenActivity() =>
        ActivitySource.StartActivity($"Authentication.Refresh");
}
