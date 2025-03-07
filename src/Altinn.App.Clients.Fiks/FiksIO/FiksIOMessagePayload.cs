using System.Text;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1.Helpers;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using KS.Fiks.IO.Crypto.Models;
using Kode = KS.Fiks.IO.Arkiv.Client.ForenkletArkivering.Kode;

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

    internal ForenkletDokument ToForenkletDokument(KS.Fiks.Arkiv.Models.V1.Kodelister.Kode code)
    {
        return new ForenkletDokument
        {
            dokumenttype = new Kode { kodeverdi = code.Verdi, kodebeskrivelse = code.Beskrivelse },
            filnavn = Filename,
            tittel = Filename,
            referanseDokumentFil = Filename,
            systemID = "Altinn",
        };
    }

    private sealed record PayloadWrapper(string Filename, Stream Payload) : IPayload;
}
