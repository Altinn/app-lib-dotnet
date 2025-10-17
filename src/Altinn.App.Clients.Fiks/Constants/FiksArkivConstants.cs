namespace Altinn.App.Clients.Fiks.Constants;

/// <summary>
/// Constants related to Fiks Arkiv.
/// </summary>
public static class FiksArkivConstants
{
    /// <summary>
    /// The name of the Fiks Arkiv record document.<br /><br />
    /// Re: NOARK-5 specification <see href="https://docs.digdir.no/docs/eFormidling/Utvikling/Dokumenttyper/standard_arkivmelding"/>
    /// </summary>
    public const string ArchiveRecordFilename = "arkivmelding.xml";
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
