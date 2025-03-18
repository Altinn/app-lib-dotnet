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
}
