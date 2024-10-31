using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningDelegationService
{
    internal Task<List<SigneeContext>> DelegateSigneeRights(
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    );
}
