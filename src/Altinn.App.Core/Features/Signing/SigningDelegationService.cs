using System.Globalization;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Builders;
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
                    DelegationRequest delegationRequest = DelegationBuilder
                        .Create()
                        .WithApplicationId(appIdentifier)
                        .WithInstanceId(actualInstanceId)
                        .WithDelegator(new Delegator { IdType = DelegationConst.Party, Id = instanceOwnerPartyId }) // TODO: should it be possible for other than the instance owner to delegate rights?
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
                                            new Resource { Value = appResourceId.Value },
                                            new Resource { Type = DelegationConst.Task, Value = taskId },
                                        ]
                                    )
                                    .Build(),
                                AccessRightBuilder
                                    .Create()
                                    .WithAction(ActionType.Sign)
                                    .WithResources(
                                        [
                                            new Resource { Value = appResourceId.Value },
                                            new Resource { Type = DelegationConst.Task, Value = taskId },
                                        ]
                                    )
                                    .Build(),
                            ]
                        )
                        .Build();
                    logger.LogInformation($"------------------------------------------------------------------------");
                    logger.LogInformation($"with application id: {appResourceId}, and instance id: {actualInstanceId}");
                    logger.LogInformation(
                        $"from id type: {delegationRequest.From?.IdType}, id: {delegationRequest.From?.Id}"
                    );
                    logger.LogInformation(
                        $"to id type: {delegationRequest.To?.IdType}, id: {delegationRequest.To?.Id}"
                    );
                    logger.LogInformation(
                        $"rights 1: action type - {delegationRequest.Rights?[0].Action?.Value}, value - {delegationRequest.Rights?[0].Action?.Type}"
                    );
                    logger.LogInformation(
                        $"rights 1: resource 1 - type - {delegationRequest.Rights?[0].Resource?[0].Type}, value - {delegationRequest.Rights?[0].Resource?[0].Value}"
                    );
                    logger.LogInformation(
                        $"rights 1: resource 2 - type - {delegationRequest.Rights?[0].Resource?[1].Type}, value - {delegationRequest.Rights?[0].Resource?[1].Value}"
                    );
                    logger.LogInformation(
                        $"rights 2: action type - {delegationRequest.Rights?[1].Action?.Value}, value - {delegationRequest.Rights?[1].Action?.Type}"
                    );
                    logger.LogInformation(
                        $"rights 2: resource 1 - type - {delegationRequest.Rights?[1].Resource?[0].Type}, value - {delegationRequest.Rights?[1].Resource?[0].Value}"
                    );
                    logger.LogInformation(
                        $"rights 2: resource 2 - type - {delegationRequest.Rights?[1].Resource?[1].Type}, value - {delegationRequest.Rights?[1].Resource?[1].Value}"
                    );
                    logger.LogInformation($"------------------------------------------------------------------------");
                    var response = await accessManagementClient.DelegateRights(delegationRequest, ct);
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
