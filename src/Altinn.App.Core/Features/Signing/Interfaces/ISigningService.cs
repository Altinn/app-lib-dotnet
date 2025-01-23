using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for the signing service.
/// </summary>
public interface ISigningService
{
    /// <summary>
    /// Gets the signees for the current task from the signee provider implemented in the app.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="signatureConfiguration"></param>
    /// <returns></returns>
    Task<SigneesResult?> GetSigneesFromProvider(Instance instance, AltinnSignatureConfiguration signatureConfiguration);

    /// <summary>
    /// Creates the signee contexts for the current task.
    /// </summary>
    /// <param name="instanceMutator"></param>
    /// <param name="signatureConfiguration"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<SigneeContext>> CreateSigneeContexts(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Delegates access to the current task and notifies the signees.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="delegatorParty"></param>
    /// <param name="instanceMutator"></param>
    /// <param name="signeeContexts"></param>
    /// <param name="signatureConfiguration"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<SigneeContext>> DelegateAccessAndNotifySignees(
        string taskId,
        Party delegatorParty,
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Gets the signee contexts for the current task.
    /// </summary>
    /// <param name="instanceDataAccessor"></param>
    /// <param name="signatureConfiguration"></param>
    /// <returns></returns>
    Task<List<SigneeContext>> GetSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    );

    /// <summary>
    /// Signs the current task.
    /// </summary>
    /// <param name="userActionContext"></param>
    /// <param name="currentTask"></param>
    /// <returns></returns>
    Task Sign(UserActionContext userActionContext, ProcessTask currentTask);
}
