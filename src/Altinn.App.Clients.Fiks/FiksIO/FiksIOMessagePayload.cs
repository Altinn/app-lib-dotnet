using KS.Fiks.IO.Crypto.Models;

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

    public string GetDotlessFileExtension(bool upperCase = true)
    {
        var extension = Path.GetExtension(Filename) is { Length: > 1 } ext ? ext[1..] : Filename;

        return upperCase ? extension.ToUpperInvariant() : extension;
    }

    internal IPayload ToPayload()
    {
        return new PayloadWrapper(Filename, Data);
    }

    private sealed record PayloadWrapper(string Filename, Stream Payload) : IPayload;
}
