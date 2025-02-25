using KS.Fiks.IO.Crypto.Models;

namespace Altinn.App.Clients.Fiks.FiksIO;

public record FiksIOMessagePayload
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

    internal IPayload ToPayload()
    {
        return new PayloadWrapper(Filename, Data);
    }

    private record PayloadWrapper(string Filename, Stream Payload) : IPayload;
}
