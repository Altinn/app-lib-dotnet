using System.Globalization;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Models;
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
        string instanceOwnerPartyId,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    )
    {
        logger.LogInformation($"------------------------------------------------------------------------");
        var actualInstanceId = instanceId.Split("/")[1];
        var appResourceId = AppResourceId.FromAppIdentifier(appIdentifier);
        // log appIdentifier and appResourceId
        logger.LogInformation($"AppIdentifier: {appIdentifier.Org}/{appIdentifier.App}");
        logger.LogInformation($"AppResourceId: {appResourceId.Value}");
        bool success = true;
        logger.LogInformation($"------------------------------------------------------------------------");
        logger.LogInformation($"Delegating signee rights for task {taskId}.");
        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SigneeState state = signeeContext.SigneeState;

            try
            {
                if (state.IsAccessDelegated is false)
                {
                    var dr = new DelegationRequest
                    {
                        To = new Delegatee
                        {
                            Id = signeeContext.PartyId.ToString(CultureInfo.InvariantCulture),
                            IdType = DelegationConst.Party,
                        },
                        From = new Delegator { Id = instanceOwnerPartyId, IdType = DelegationConst.Party },
                        ResourceId = appResourceId.Value,
                        InstanceId = actualInstanceId,
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
