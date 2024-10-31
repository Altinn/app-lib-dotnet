using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService(IAccessManagementClient accessManagementClient)
    : ISigningDelegationService
{
    public async Task<List<SigneeContext>> DelegateSigneeRights(
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    )
    {
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;
            try
            {
                if (state.IsAccessDelegated is false)
                {
                    // csharpier-ignore-start
                    string appResourceId = instance.AppId;
                    DelegationRequest delegation = DelegationRequestBuilder
                        .CreateBuilder()
                        .WithAppResourceId(appResourceId) // TODO: translate app id to altinn resource id
                        .WithInstanceId(instance.Id)
                        .WithDelegator(new From { Type = DelegationConst.Party, Value = FromPartyId })
                        .WithRecipient(new To { Type = DelegationConst.Party, Value = ToPartyId })
                        .AddRight()
                            .WithAction(DelegationConst.ActionId, ActionType.Read)
                            .AddResource(DelegationConst.Resource, appResourceId) // TODO: translate app id to altinn resource id
                            .AddResource(DelegationConst.Task, taskId)
                            .BuildRight()
                        .AddRight()
                            .WithAction(DelegationConst.ActionId, ActionType.Sign)
                            .AddResource(DelegationConst.Resource, appResourceId) // TODO: translate app id to altinn resource id
                            .AddResource(DelegationConst.Task, taskId)
                            .BuildRight()
                        .Build();
                    // csharpier-ignore-end
                    var response = await accessManagementClient.DelegateRights(delegation, ct);
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
