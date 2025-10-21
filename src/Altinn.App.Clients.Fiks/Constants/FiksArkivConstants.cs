using KS.Fiks.Arkiv.Models.V1.Meldingstyper;

namespace Altinn.App.Clients.Fiks.Constants;

/// <summary>
/// Constants related to Fiks Arkiv.
/// </summary>
public static class FiksArkivConstants
{
    /// <summary>
    /// The name of the Fiks Arkiv record document (as per <see href="https://developers.fiks.ks.no/tjenester/fiksprotokoll/protokoll-arkiv/#meldinger">Fiks Protokoll specifications</see>).
    /// </summary>
    public const string ArchiveRecordFilename = "arkivmelding.xml";

    internal const string ReceiptMessageType = FiksArkivMeldingtype.ArkivmeldingOpprettKvittering;
    internal const string AltinnSystemId = "Altinn Studio";
    internal const string AltinnOrgNo = "991825827";

    internal static class ClassificationId
    {
        public const string NationalIdentityNumber = "Fødselsnummer";
        public const string OrganizationNumber = "Organisasjonsnummer";
        public const string AltinnUserId = "AltinnBrukerId";
        public const string SystemUserId = "SystembrukerId";
    }
}
