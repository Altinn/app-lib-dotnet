using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Telemetry.DelegationConst;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningDelegationService(
    IAccessManagementClient accessManagementClient,
    ILogger<SigningDelegationService> logger
) : ISigningDelegationService
{
    public async Task<(List<SigneeContext>, bool success)> RevokeSigneeRights(
        string taskId,
        string instanceId,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        var instanceGuid = instanceId.Split("/")[1];
        var appResourceId = AppResourceId.FromAppIdentifier(appIdentifier);
        bool success = true;
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            if (signeeContext.SigneeState.IsAccessDelegated is true)
            {
                try
                {
                    DelegationRequest delegationRequest = new()
                    {
                        ResourceId = appResourceId.Value,
                        InstanceId = instanceGuid,
                        From = new DelegationParty
                        {
                            Value =
                                delegatorParty.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegator: PartyUuid is null"),
                        },
                        To = new DelegationParty
                        {
                            Value =
                                signeeContext.Party.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegatee: PartyUuid is null"),
                        },
                        Rights =
                        [
                            new RightRequest
                            {
                                Resource =
                                [
                                    new AppResource { Value = appIdentifier.App },
                                    new OrgResource { Value = appIdentifier.Org },
                                    new TaskResource { Value = taskId },
                                ],
                                Action = new AltinnAction { Value = ActionType.Read },
                            },
                            new RightRequest
                            {
                                Resource =
                                [
                                    new AppResource { Value = appIdentifier.App },
                                    new OrgResource { Value = appIdentifier.Org },
                                    new TaskResource { Value = taskId },
                                ],
                                Action = new AltinnAction { Value = ActionType.Sign },
                            },
                        ],
                    };
                    DelegationResponse? response = await accessManagementClient.RevokeRights(delegationRequest, ct);
                    signeeContext.SigneeState.IsAccessDelegated = false;
                    telemetry?.RecordDelegationRevoke(DelegationResult.Success);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to revoke signee rights");
                    signeeContext.SigneeState.DelegationFailedReason = "Failed to revoke signee rights: " + ex.Message;
                    telemetry?.RecordDelegationRevoke(DelegationResult.Error);
                    success = false;
                }
            }
        }
        return (signeeContexts, success);
    }

    public async Task<(List<SigneeContext>, bool success)> DelegateSigneeRights(
        string taskId,
        string instanceId,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        var instanceGuid = instanceId.Split("/")[1];
        var appResourceId = AppResourceId.FromAppIdentifier(appIdentifier);
        bool success = true;

        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;

            try
            {
                if (state.IsAccessDelegated is false)
                {
                    logger.LogInformation(
                        $"Delegating signee rights to {signeeContext.Party.PartyUuid} from {delegatorParty.PartyUuid} for {appResourceId.Value}"
                    );
                    DelegationRequest delegationRequest = new()
                    {
                        ResourceId = appResourceId.Value,
                        InstanceId = instanceGuid,
                        From = new DelegationParty
                        {
                            Value =
                                delegatorParty.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegator: PartyUuid is null"),
                        },
                        To = new DelegationParty
                        {
                            Value =
                                signeeContext.Party.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegatee: PartyUuid is null"),
                        },
                        Rights =
                        [
                            new RightRequest
                            {
                                Resource =
                                [
                                    new AppResource { Value = appIdentifier.App },
                                    new OrgResource { Value = appIdentifier.Org },
                                    new TaskResource { Value = taskId },
                                ],
                                Action = new AltinnAction { Value = ActionType.Read },
                            },
                            new RightRequest
                            {
                                Resource =
                                [
                                    new AppResource { Value = appIdentifier.App },
                                    new OrgResource { Value = appIdentifier.Org },
                                    new TaskResource { Value = taskId },
                                ],
                                Action = new AltinnAction { Value = ActionType.Sign },
                            },
                        ],
                    };
                    DelegationResponse? response = await accessManagementClient.DelegateRights(delegationRequest, ct);
                    state.IsAccessDelegated = true;
                    telemetry?.RecordDelegation(DelegationResult.Success);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delegate signee rights");
                state.DelegationFailedReason = "Failed to delegate signee rights: " + ex.Message;
                telemetry?.RecordDelegation(DelegationResult.Error);
                success = false;
            }
        }

        return (signeeContexts, success);
    }
}
