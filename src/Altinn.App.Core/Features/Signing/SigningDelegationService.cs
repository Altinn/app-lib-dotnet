using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService() : ISigningDelegationService
{
    public async Task DelegateSigneeRights(List<SigneeContext> signeeContexts, CancellationToken ct)
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            try
            {
                if (state.IsDelegated is false)
                {
                    //TODO: delegateSignAction
                    state.IsDelegated = true;
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }
    }
}
