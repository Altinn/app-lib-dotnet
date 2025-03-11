using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal static class FiksArkivConstants
{
    public const string AltinnSystemLabel = "Altinn";
    public const string AltinnOrgNo = "991825827";
    public const string ArchiveRecordFilename = "arkivmelding.xml";

    public static AdministrativEnhet AdministrativEnhet => new() { Navn = AltinnSystemLabel };
}
