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
    public async Task<List<SigneeContext>> RevokeSigneeRights(
        string taskId,
        string instanceId,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        var updatedContexts = new List<SigneeContext>();

        foreach (SigneeContext signeeContext in signeeContexts)
        {
            try
            {
                if (signeeContext.SigneeState.IsAccessDelegated is false)
                {
                    updatedContexts.Add(signeeContext);
                    continue;
                }

                await RevokeRightsForSingleSignee(
                    taskId,
                    instanceGuid: instanceId.Split("/")[1],
                    delegatorParty,
                    appIdentifier,
                    appResourceId: AppResourceId.FromAppIdentifier(appIdentifier),
                    signeeContext,
                    ct,
                    telemetry
                );
                signeeContext.SigneeState.DelegationFailedReason = null;
                signeeContext.SigneeState.IsAccessDelegated = false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to revoke signee rights");
                signeeContext.SigneeState.DelegationFailedReason = "Failed to revoke signee rights: " + ex.Message;
                telemetry?.RecordDelegationRevoke(DelegationResult.Error);
            }
        }
        return signeeContexts;
    }

    public async Task<List<SigneeContext>> DelegateSigneeRights(
        string taskId,
        string instanceId,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        var updatedContexts = new List<SigneeContext>();

        foreach (SigneeContext signeeContext in signeeContexts)
        {
            if (signeeContext.SigneeState.IsAccessDelegated is true)
            {
                updatedContexts.Add(signeeContext);
                continue;
            }

            try
            {
                await DelegateRightsForSingleSignee(
                    taskId,
                    instanceGuid: instanceId.Split("/")[1],
                    delegatorParty,
                    appIdentifier,
                    appResourceId: AppResourceId.FromAppIdentifier(appIdentifier),
                    signeeContext,
                    ct,
                    telemetry
                );
                signeeContext.SigneeState.DelegationFailedReason = null;
                signeeContext.SigneeState.IsAccessDelegated = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delegate signee rights");
                signeeContext.SigneeState.DelegationFailedReason = "Failed to delegate signee rights: " + ex.Message;
                signeeContext.SigneeState.IsAccessDelegated = false;
                telemetry?.RecordDelegation(DelegationResult.Error);
            }
            finally
            {
                updatedContexts.Add(signeeContext);
            }
        }

        return updatedContexts;
    }

    private async Task RevokeRightsForSingleSignee(
        string taskId,
        string instanceGuid,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        AppResourceId appResourceId,
        SigneeContext signeeContext,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        logger.LogInformation(
            $"Revoking signee rights from {signeeContext.Party.PartyUuid} from {delegatorParty.PartyUuid} for {appResourceId.Value}"
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
        await accessManagementClient.RevokeRights(delegationRequest, ct);
        telemetry?.RecordDelegationRevoke(DelegationResult.Success);
    }

    private async Task DelegateRightsForSingleSignee(
        string taskId,
        string instanceGuid,
        Party delegatorParty,
        AppIdentifier appIdentifier,
        AppResourceId appResourceId,
        SigneeContext signeeContext,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
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
        await accessManagementClient.DelegateRights(delegationRequest, ct);
        telemetry?.RecordDelegation(DelegationResult.Success);
    }
}
