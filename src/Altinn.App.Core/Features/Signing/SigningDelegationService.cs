using System.Globalization;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Builders;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.Platform.Storage.Interface.Models;
using static Altinn.App.Core.Features.Telemetry.DelegationConst;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService(IAccessManagementClient accessManagementClient)
    : ISigningDelegationService
{
    public async Task<(List<SigneeContext>, bool success)> DelegateSigneeRights(
        string taskId,
        Instance instance,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        bool success = true;
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;

            try
            {
                if (state.IsAccessDelegated is false)
                {
                    DelegationRequest delegationRequest = DelegationBuilder
                        .Create()
                        .WithApplicationId(instance.AppId)
                        .WithInstanceId(instance.Id)
                        .WithDelegator(
                            new Delegator { IdType = DelegationConst.Party, Id = instance.InstanceOwner.PartyId }
                        ) // TODO: should it be possible for other than the instance owner to delegate rights?
                        .WithDelegatee(
                            new Delegatee
                            {
                                IdType = DelegationConst.Party,
                                Id = signeeContext.PartyId.ToString(CultureInfo.InvariantCulture),
                            }
                        )
                        .WithRights(
                            [
                                AccessRightBuilder
                                    .Create()
                                    .WithAction(ActionType.Read)
                                    .WithResources(
                                        [
                                            new Resource { Value = AppIdHelper.ToResourceId(instance.AppId) },
                                            new Resource { Type = DelegationConst.Task, Value = taskId },
                                        ]
                                    )
                                    .Build(),
                                AccessRightBuilder
                                    .Create()
                                    .WithAction(ActionType.Sign)
                                    .WithResources(
                                        [
                                            new Resource { Value = AppIdHelper.ToResourceId(instance.AppId) },
                                            new Resource { Type = DelegationConst.Task, Value = taskId },
                                        ]
                                    )
                                    .Build(),
                            ]
                        )
                        .Build();
                    var response = await accessManagementClient.DelegateRights(delegationRequest, ct);
                    state.IsAccessDelegated = await Task.FromResult(true);
                    telemetry?.RecordDelegation(DelegationResult.Success);
                }
            }
            catch (Exception ex)
            {
                state.DelegationFailedReason = "Failed to delegate signee rights: " + ex.Message;
                telemetry?.RecordDelegation(DelegationResult.Error);
                success = false;
            }
        }

        return (signeeContexts, success);
    }
}
