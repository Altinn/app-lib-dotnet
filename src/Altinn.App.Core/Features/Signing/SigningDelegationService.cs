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
                    var dr = new DelegationRequest
                    {
                        From = new Delegator
                        {
                            Type = DelegationConst.Party,
                            Value =
                                delegatorParty.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegator: PartyUuid is null"),
                        },
                        To = new Delegatee
                        {
                            Type = DelegationConst.Party,
                            Value =
                                signeeContext.Party.PartyUuid.ToString()
                                ?? throw new InvalidOperationException("Delegatee: PartyUuid is null"),
                        },
                        ResourceId = appResourceId.Value,
                        InstanceId = instanceGuid,
                        Rights =
                        [
                            new RightRequest
                            {
                                Resource =
                                [
                                    new Resource { Type = DelegationConst.Resource, Value = appResourceId.Value },
                                    new Resource { Type = DelegationConst.Task, Value = taskId },
                                ],
                                Action = new AltinnAction { Type = DelegationConst.ActionId, Value = "read" },
                            },
                            new RightRequest
                            {
                                Resource =
                                [
                                    new Resource { Type = DelegationConst.Resource, Value = appResourceId.Value },
                                    new Resource { Type = DelegationConst.Task, Value = taskId },
                                ],
                                Action = new AltinnAction { Type = DelegationConst.ActionId, Value = "sign" },
                            },
                        ],
                    };
                    var response = await accessManagementClient.DelegateRights(dr, ct);
                    state.IsAccessDelegated = await Task.FromResult(true);
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
