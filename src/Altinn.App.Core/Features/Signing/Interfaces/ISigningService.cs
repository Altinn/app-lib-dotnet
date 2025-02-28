using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.UserAction;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for the signing service.
/// </summary>
public interface ISigningService
{
    /// <summary>
    /// Creates the signee contexts for the current task.
    /// </summary>
    Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Delegates access to the current task, notifies the signees about
    /// a new task to sign and saves the signee contexts to Storage.
    /// </summary>
    Task<List<SigneeContext>> InitialiseSignees(
        string taskId,
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Gets the signee contexts for the current task.
    /// </summary>
    Task<List<SigneeContext>> GetSigneeContexts(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    );

    /// <summary>
    /// Signs the current task.
    /// </summary>
    Task Sign(UserActionContext userActionContext, ProcessTask currentTask);

    /// <summary>
    /// Aborts runtime delegated signing. Deletes all signing data and revokes delegated access.
    /// </summary>
    Task AbortRuntimeDelegatedSigning(
        string taskId,
        IInstanceDataMutator instanceDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );
}
