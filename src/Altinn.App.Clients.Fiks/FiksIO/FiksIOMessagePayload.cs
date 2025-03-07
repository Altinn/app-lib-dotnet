using System.Text;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1.Helpers;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using KS.Fiks.IO.Crypto.Models;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksIO;

public sealed record FiksIOMessagePayload
{
    public Stream Data { get; init; }
    public string Filename { get; init; }

    public FiksIOMessagePayload(string filename, Stream data)
    {
        Data = data;
        Filename = filename;
    }

    public FiksIOMessagePayload(string filename, ReadOnlyMemory<byte> data)
    {
        Data = new MemoryStream(data.ToArray());
        Filename = filename;
    }

    public FiksIOMessagePayload(string filename, object xmlSerializableData)
    {
        var serialized = ArkivmeldingSerializeHelper.Serialize(xmlSerializableData);
        var data = Encoding.UTF8.GetBytes(serialized);

        Data = new MemoryStream(data);
        Filename = filename;
    }

    internal IPayload ToPayload()
    {
        return new PayloadWrapper(Filename, Data);
    }

    internal Dokumentbeskrivelse ToDokumentbeskrivelse(Kode documentType, Guid fiksAccountId)
    {
        var associatedDocumentType =
            documentType == DokumenttypeKoder.Dokument
                ? TilknyttetRegistreringSomKoder.Hoveddokument
                : TilknyttetRegistreringSomKoder.Vedlegg;

        var dokumentBeskrivelse = new Dokumentbeskrivelse
        {
            Dokumenttype = new Dokumenttype
            {
                KodeProperty = documentType.Verdi,
                Beskrivelse = documentType.Beskrivelse,
            },
            Dokumentstatus = new Dokumentstatus
            {
                KodeProperty = DokumentstatusKoder.Ferdig.Verdi,
                Beskrivelse = DokumentstatusKoder.Ferdig.Beskrivelse,
            },
            Tittel = Filename,
            TilknyttetRegistreringSom = new TilknyttetRegistreringSom
            {
                KodeProperty = associatedDocumentType.Verdi,
                Beskrivelse = associatedDocumentType.Beskrivelse,
            },
            OpprettetDato = DateTime.Now,
        };

        var dotlessFileExtension = Path.GetExtension(Filename) is { Length: > 1 } ext ? ext[1..] : Filename;

        dokumentBeskrivelse.Dokumentobjekt.Add(
            new Dokumentobjekt
            {
                SystemID = new SystemID { Value = fiksAccountId.ToString(), Label = "Altinn" },
                Filnavn = Filename,
                ReferanseDokumentfil = Filename,
                Format = new Format { KodeProperty = dotlessFileExtension.ToUpperInvariant() },
                Variantformat = new Variantformat
                {
                    KodeProperty = VariantformatKoder.Produksjonsformat.Verdi,
                    Beskrivelse = VariantformatKoder.Produksjonsformat.Beskrivelse,
                },
            }
        );

        return dokumentBeskrivelse;
    }

    private sealed record PayloadWrapper(string Filename, Stream Payload) : IPayload;
}
