using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningDelegationService
{
    internal Task<(List<SigneeContext>, bool success)> DelegateSigneeRights(
        string taskId,
        string instanceId,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    );
}
