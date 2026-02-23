using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Resolver for Fiks Arkiv configuration values.
/// </summary>
public interface IFiksArkivConfigResolver
{
    /// <summary>
    /// Settings related to the primary document for the Fiks Arkiv shipment.
    /// </summary>
    FiksArkivDataTypeSettings PrimaryDocumentSettings { get; }

    /// <summary>
    /// Settings related to the attachments for the Fiks Arkiv shipment.
    /// </summary>
    IReadOnlyList<FiksArkivDataTypeSettings> AttachmentSettings { get; }

    /// <summary>
    /// Gets the title of the current application, resolved through applicable text resources if available.
    /// </summary>
    Task<string> GetApplicationTitle(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the archive document metadata (title, etc).
    /// </summary>
    /// <param name="dataAccessor">The data accessor providing access to instance data.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<FiksArkivDocumentMetadata?> GetArchiveDocumentMetadata(
        IInstanceDataAccessor dataAccessor,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the recipient information for the shipment.
    /// </summary>
    /// <param name="dataAccessor">The data accessor providing access to instance data.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<FiksArkivRecipient> GetRecipient(
        IInstanceDataAccessor dataAccessor,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the correlation ID for the shipment.
    /// </summary>
    string GetCorrelationId(Instance instance);

    /// <summary>
    /// Gets the recipient party (korrespondansepart).
    /// </summary>
    Korrespondansepart GetRecipientParty(Instance instance, FiksArkivRecipient recipient);

    /// <summary>
    /// Gets the service owner party (korrespondansepart).
    /// </summary>
    Task<Korrespondansepart> GetServiceOwnerParty(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the instance owner party (korrespondansepart).
    /// </summary>
    Task<Korrespondansepart?> GetInstanceOwnerParty(Instance instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the classification of the instance owner (klassifikasjon).
    /// </summary>
    Task<Klassifikasjon> GetInstanceOwnerClassification(
        Authenticated auth,
        CancellationToken cancellationToken = default
    );
}
