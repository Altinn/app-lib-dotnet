using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService(IAccessManagementClient accessManagementClient)
    : ISigningDelegationService
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
                    string taskId = "123"; // TODO: do not hardcode this..
                    Instance instance = new(); // TODO: do not hardcode this..
                    await accessManagementClient.DelegateSignRights(taskId, instance);
                    state.IsAccessDelegated = await Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                state.DelegationFailedReason = "Failed to delegate signee rights: " + ex.Message;
            }
        }

        return signeeContexts;
    }
}
