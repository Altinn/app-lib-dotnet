using Altinn.App.Clients.Fiks.Extensions;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace Altinn.App.Clients.Fiks.Factories;

internal static class KorrespondansepartFactory
{
    /// <summary>
    /// Creates a Korrespondansepart of type Avsender (Sender).
    /// </summary>
    /// <remarks>
    /// <c>partyName</c> and <c>partyId</c> are nullable only for caller convenience.
    /// Null or empty values for these parameters will result in a FiksArkivConfigurationException.
    /// </remarks>
    public static Korrespondansepart CreateSender(
        string? partyName,
        string? partyId,
        string? organizationId = null,
        string? personId = null,
        string? reference = null
    ) =>
        new()
        {
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Avsender.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Avsender.Beskrivelse,
            },
            KorrespondansepartID = partyId.EnsureNotNullOrEmpty("FiksArkiv->Sender.ID"),
            KorrespondansepartNavn = partyName.EnsureNotNullOrEmpty("FiksArkiv->Sender.Name"),
            Organisasjonid = organizationId.EnsureNotEmpty("FiksArkiv->Sender.OrganizationId"),
            Personid = personId.EnsureNotEmpty("FiksArkiv->Sender.PersonId"),
            DeresReferanse = reference.EnsureNotEmpty("FiksArkiv->Sender.Reference"),
        };

    public static Korrespondansepart CreateInternalSender(string partyName, string partyId) =>
        new()
        {
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.InternAvsender.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.InternAvsender.Beskrivelse,
            },
            KorrespondansepartNavn = partyName.EnsureNotNullOrEmpty("FiksArkiv->InternalSender.ID"),
            KorrespondansepartID = partyId.EnsureNotNullOrEmpty("FiksArkiv->InternalSender.Name"),
        };

    /// <summary>
    /// Creates a Korrespondansepart of type Mottaker (Recipient).
    /// </summary>
    /// <remarks>
    /// <c>partyName</c> and <c>partyId</c> are nullable only for caller convenience.
    /// Null or empty values for these parameters will result in a FiksArkivConfigurationException.
    /// </remarks>
    public static Korrespondansepart CreateRecipient(
        string? partyName,
        string? partyId,
        string? organizationId = null,
        string? personId = null,
        string? reference = null
    ) =>
        new()
        {
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Mottaker.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Mottaker.Beskrivelse,
            },
            KorrespondansepartID = partyId.EnsureNotNullOrEmpty("FiksArkiv->Recipient.ID"),
            KorrespondansepartNavn = partyName.EnsureNotNullOrEmpty("FiksArkiv->Recipient.Name"),
            Organisasjonid = organizationId.EnsureNotEmpty("FiksArkiv->Recipient.OrganizationId"),
            Personid = personId.EnsureNotEmpty("FiksArkiv->Recipient.PersonId"),
            DeresReferanse = reference.EnsureNotEmpty("FiksArkiv->Recipient.Reference"),
        };
}
