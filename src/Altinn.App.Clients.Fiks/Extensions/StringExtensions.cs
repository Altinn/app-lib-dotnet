using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class StringExtensions
{
    public static T? DeserializeXml<T>(this string xml)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        var serializer = new XmlSerializer(typeof(T));
        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader);

        return serializer.Deserialize(xmlReader) as T;
    }

    public static string ToUrlSafeBase64(this string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        // Convert to standard Base64 string.
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        var base64 = Convert.ToBase64String(plainTextBytes);

        // Make the string URL safe.
        base64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return base64;
    }

    public static string FromUrlSafeBase64(this string base64Encoded)
    {
        ArgumentNullException.ThrowIfNull(base64Encoded);

        // Convert the URL safe string back to a standard Base64 format.
        base64Encoded = base64Encoded.Replace('-', '+').Replace('_', '/');

        // Calculate the number of padding characters needed.
        int padding = 4 - (base64Encoded.Length % 4);
        if (padding != 4)
        {
            base64Encoded = base64Encoded.PadRight(base64Encoded.Length + padding, '=');
        }

        var base64EncodedBytes = Convert.FromBase64String(base64Encoded);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}
