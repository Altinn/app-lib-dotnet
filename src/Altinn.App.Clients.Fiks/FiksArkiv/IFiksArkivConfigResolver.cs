using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Features.Auth;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public interface IFiksArkivConfigResolver
{
    FiksArkivDataTypeSettings PrimaryDocumentSettings { get; }
    IReadOnlyList<FiksArkivDataTypeSettings> AttachmentSettings { get; }
    Task<string> GetApplicationTitle();
    Task<FiksArkivDocumentMetadata?> GetConfigMetadata(Instance instance);
    Task<FiksArkivRecipient> GetRecipient(Instance instance);
    string GetCorrelationId(Instance instance);
    Korrespondansepart? GetRecipientParty(Instance instance, FiksArkivRecipient recipient);
    Task<Korrespondansepart> GetServiceOwnerParty();
    Task<Korrespondansepart?> GetInstanceOwnerParty(Instance instance);
    Task<Klassifikasjon> GetFormSubmitterClassification(Authenticated auth);
}
