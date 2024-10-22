using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing;

internal interface ISigningDelegationService
{
    internal Task DelegateSigneeRights(List<SigneeContext> signeeContexts, CancellationToken ct);
}
