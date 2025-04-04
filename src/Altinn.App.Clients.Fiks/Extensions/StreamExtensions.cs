namespace Altinn.App.Clients.Fiks.Extensions;

internal static class StreamExtensions
{
    public static string ReadToString(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
