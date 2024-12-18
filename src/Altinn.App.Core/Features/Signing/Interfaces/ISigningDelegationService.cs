using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningDelegationService
{
    internal Task<(List<SigneeContext>, bool success)> DelegateSigneeRights(
        string taskId,
        string instanceId,
        string instanceOwnerPartyId,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    );
}
