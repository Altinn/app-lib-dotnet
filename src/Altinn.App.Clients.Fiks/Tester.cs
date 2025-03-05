using System.Xml.Serialization;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;

namespace Altinn.App.Clients.Fiks;

public class Tester
{
    public void Test()
    {
        var factory = new ArkivmeldingFactory();

        var referanseSaksmappeForenklet = new SaksmappeForenklet
        {
            saksaar = 2025,
            sakssekvensnummer = 0,
            mappetype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
            saksdato = DateTime.UtcNow,
            tittel = "tittel",
            administrativEnhet = "enhet",
            referanseAdministrativEnhet = "ref",
            offentligTittel = "offentlig",
            saksansvarlig = "ansvarlig",
            referanseSaksansvarlig = "ref",
            saksstatus = "status",
            avsluttetAv = "avsluttet",
            skjermetTittel = false,
            referanseEksternNoekkelForenklet = new EksternNoekkelForenklet { fagsystem = "system", noekkel = "nokkel" },
            klasse =
            [
                new KlasseForenklet
                {
                    klasseID = "id",
                    klassifikasjonssystem = "system",
                    skjermetKlasse = false,
                    tittel = "tittel",
                },
            ],
        };

        var nyUtgaaendeJournalpost = new UtgaaendeJournalpost
        {
            journalaar = 2025,
            journalsekvensnummer = 0,
            journalpostnummer = 0,
            dokumentetsDato = DateTime.UtcNow,
            sendtDato = DateTime.UtcNow,
            hoveddokument = new ForenkletDokument
            {
                dokumenttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                filnavn = "filnavn",
                tittel = "tittel",
                skjermetDokument = false,
                referanseDokumentFil = "ref",
                systemID = "id",
            },
            skjermetTittel = false,
            offentligTittel = "offentlig",
            referanseEksternNoekkelForenklet = new EksternNoekkelForenklet
            {
                fagsystem = "fagsystem",
                noekkel = "noekkel",
            },
            tittel = "tittel",
            vedlegg =
            [
                new ForenkletDokument
                {
                    dokumenttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                    filnavn = "filnavn",
                    tittel = "tittel",
                    skjermetDokument = false,
                    referanseDokumentFil = "ref",
                    systemID = "id",
                },
            ],
            mottaker =
            [
                new KorrespondansepartForenklet
                {
                    systemID = "id",
                    enhetsidentifikator = new Enhetsidentifikator { organisasjonsnummer = "orgnr", landkode = "kode" },
                    personid = new Personidentifikator
                    {
                        personidentifikatorNr = "fnr",
                        personidentifikatorLandkode = "kode",
                    },
                    korrespondanseparttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                    navn = "navn",
                    skjermetKorrespondansepart = false,
                    postadresse = null,
                    kontaktinformasjonForenklet = new KontaktinformasjonForenklet
                    {
                        epostadresse = "epost",
                        mobiltelefon = "nummer",
                        telefon = "nummer",
                    },
                    kontaktperson = "person",
                    deresReferanse = "deresreferanse",
                    forsendelsemåte = "måte",
                },
            ],
            avsender =
            [
                new KorrespondansepartForenklet
                {
                    systemID = "id",
                    enhetsidentifikator = new Enhetsidentifikator { organisasjonsnummer = "orgnr", landkode = "kode" },
                    personid = new Personidentifikator
                    {
                        personidentifikatorNr = "fnr",
                        personidentifikatorLandkode = "kode",
                    },
                    korrespondanseparttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                    navn = "navn",
                    skjermetKorrespondansepart = false,
                    postadresse = null,
                    kontaktinformasjonForenklet = new KontaktinformasjonForenklet
                    {
                        epostadresse = "epost",
                        mobiltelefon = "nummer",
                        telefon = "nummer",
                    },
                    kontaktperson = "person",
                    deresReferanse = "deresreferanse",
                    forsendelsemåte = "måte",
                },
            ],
            internAvsender =
            [
                new KorrespondansepartIntern
                {
                    administrativEnhet = "enhet",
                    referanseAdministrativEnhet = "ref",
                    saksbehandler = "behandler",
                    referanseSaksbehandler = "ref",
                },
            ],
        };

        var forenkletUtgaaende = new ArkivmeldingForenkletUtgaaende
        {
            sluttbrukerIdentifikator = "",
            referanseSaksmappeForenklet = referanseSaksmappeForenklet,
            nyUtgaaendeJournalpost = nyUtgaaendeJournalpost,
        };

        var arkivmelding = factory.GetArkivmelding(forenkletUtgaaende);
        StringWriter logWriter = new StringWriter();
        XmlSerializer logSerializer = new XmlSerializer(typeof(Arkivmelding));
        logSerializer.Serialize(logWriter, arkivmelding);
        _logger.LogWarning(logWriter.ToString());
    }
}
