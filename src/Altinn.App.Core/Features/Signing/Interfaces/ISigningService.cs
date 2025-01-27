using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for the signing service.
/// </summary>
public interface ISigningService
{
    /// <summary>
    /// Creates the signee contexts for the current task.
    /// </summary>
    /// <param name="instanceMutator"></param>
    /// <param name="signatureConfiguration"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Delegates access to the current task, notifies the signees about
    /// a new task to sign and saves the signee contexts to Storage.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="delegatorParty"></param>
    /// <param name="instanceMutator"></param>
    /// <param name="signeeContexts"></param>
    /// <param name="signatureConfiguration"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<List<SigneeContext>> InitialiseSignees(
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
