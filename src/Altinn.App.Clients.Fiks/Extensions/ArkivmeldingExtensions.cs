using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ArkivmeldingExtensions
{
    /// <summary>
    /// Serializes an archive record to XML and returns a byte array.
    /// </summary>
    public static ReadOnlyMemory<byte> SerializeXmlBytes<T>(this T archiveRecord, bool indent = false)
    {
        var serializer = new XmlSerializer(typeof(T));

        using var memoryStream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = indent,
            OmitXmlDeclaration = false,
        };

        using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
        {
            serializer.Serialize(xmlWriter, archiveRecord);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Converts an archive record to a Fiks IO message payload.
    /// </summary>
    public static FiksIOMessagePayload ToPayload(this Arkivmelding archiveRecord) =>
        new(FiksArkivConstants.Filenames.ArchiveRecord, archiveRecord.SerializeXmlBytes());
}
