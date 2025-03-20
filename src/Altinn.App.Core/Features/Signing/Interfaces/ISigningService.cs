using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using OrganisationSignee = Altinn.App.Core.Features.Signing.Models.Signee.OrganisationSignee;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningService
{
    /// <summary>
    /// Creates the signee contexts for the current task.
    /// </summary>
    Task<List<SigneeContext>> GenerateSigneeContexts(
        IInstanceDataMutator instanceDataMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    /// <summary>
    /// Delegates access to the current task, notifies the signees about
    /// a new task to sign and saves the signee contexts to Storage.
    /// </summary>
    Task<List<SigneeContext>> InitialiseSignees(
        string taskId,
        IInstanceDataMutator instanceDataMutator,
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
    /// Gets the organisation signees the current user is authorized to sign on behalf of.
    /// </summary>
    Task<List<OrganisationSignee>> GetAuthorizedOrganisationSignees(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration,
        int userId
    );

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
