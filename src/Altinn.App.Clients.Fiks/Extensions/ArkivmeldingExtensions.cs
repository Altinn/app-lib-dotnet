using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksIO;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ArkivmeldingExtensions
{
    public static ReadOnlyMemory<byte> SerializeXmlBytes(this Arkivmelding archiveRecord, bool indent = false)
    {
        var serializer = new XmlSerializer(typeof(Arkivmelding));

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

    public static FiksIOMessagePayload ToPayload(this Arkivmelding archiveRecord) =>
        new(FiksArkivConstants.ArchiveRecordFilename, archiveRecord.SerializeXmlBytes());
}
