using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService() : ISigningDelegationService
{
    public async Task<List<SigneeContext>> DelegateSigneeRights(
        List<SigneeContext> signeeContexts,
        CancellationToken? ct = null
    )
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            try
            {
                if (state.IsAccessDelegated is false)
                {
                    //TODO: delegateSignAction
                    state.IsAccessDelegated = true;
                }
            }
            catch
            {
                // TODO: log + telemetry?
            }
        }

        await Task.CompletedTask;
    }
}
