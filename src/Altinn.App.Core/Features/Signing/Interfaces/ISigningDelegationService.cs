using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for implementing app-specific logic for delegating signee rights.
/// </summary>
public interface ISigningDelegationService
{
    /// <summary>
    /// Delegate signee rights for the instance to a given party from the instance owner.
    /// </summary>
    /// <param name="taskId">The id of the Task.</param>
    /// <param name="instanceIdCombo">Instance id on the form {instanceOwnerId}/{instanceGuid}</param>
    /// <param name="InstanceOwnerPartyUuid">The party uuid of the instance owner.</param>
    /// <param name="appIdentifier">The AppIdentifier.</param>
    /// <param name="signeeContexts">The signee contexts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="telemetry">Opt-in telemetry for traces and metrics.</param>
    /// <returns>Tuple with success status and list of signee contexts.</returns>
    internal Task<(List<SigneeContext>, bool success)> DelegateSigneeRights(
        string taskId,
        string instanceIdCombo,
        Guid InstanceOwnerPartyUuid,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    );

    /// <summary>
    /// Revoke signee rights for the instance to a given party from the instance owner.
    /// </summary>
    /// <param name="taskId">The id of the Task.</param>
    /// <param name="instanceIdCombo">Instance id on the form {instanceOwnerId}/{instanceGuid}</param>
    /// <param name="InstanceOwnerPartyUuid">The party uuid of the instance owner.</param>
    /// <param name="appIdentifier">The AppIdentifier.</param>
    /// <param name="signeeContexts">The signee contexts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="telemetry">Opt-in telemetry for traces and metrics.</param>
    /// <returns>Tuple with success status and list of signee contexts.</returns>
    internal Task<(List<SigneeContext>, bool success)> RevokeSigneeRights(
        string taskId,
        string instanceIdCombo,
        Guid InstanceOwnerPartyUuid,
        AppIdentifier appIdentifier,
        List<SigneeContext> signeeContexts,
        CancellationToken ct,
        Telemetry? telemetry = null
    );
}
