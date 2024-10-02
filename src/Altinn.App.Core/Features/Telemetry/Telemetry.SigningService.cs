using System.Diagnostics;

namespace Altinn.App.Core.Features;

partial class Telemetry
{
    internal Activity? StartAssignSigneesActivity() => ActivitySource.StartActivity("SigningService.AssignSignees");
    internal Activity? StartReadSigneesActivity() => ActivitySource.StartActivity("SigningService.ReadSignees");
    internal Activity? StartSignActivity() => ActivitySource.StartActivity("SigningService.Sign"); // TODO: expand to include signee id
}
