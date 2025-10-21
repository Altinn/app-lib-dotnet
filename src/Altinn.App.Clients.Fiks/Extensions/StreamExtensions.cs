namespace Altinn.App.Clients.Fiks.Extensions;

internal static class StreamExtensions
{
    /// <summary>
    /// Reads the entire stream to a string.
    /// </summary>
    public static string ReadToString(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
