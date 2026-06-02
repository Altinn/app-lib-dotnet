using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

internal sealed record MessagePayloadWrapper(
    FiksIOMessagePayload Payload,
    Kode FileTypeCode,
    FiksArkivDocumentFormat? FileFormat,
    FiksArkivDocumentVariant? FileVariant
)
{
    public Format GetFileFormat() =>
        new()
        {
            KodeProperty = !string.IsNullOrWhiteSpace(FileFormat?.Code)
                ? FileFormat.Code
                : Payload.GetDotlessFileExtension(),
        };

    public Variantformat? GetFileVariant()
    {
        if (FileVariant is null)
        {
            return null;
        }

        return new Variantformat()
        {
            KodeProperty = FileVariant.Code,
            Beskrivelse = !string.IsNullOrWhiteSpace(FileVariant.Description) ? FileVariant.Description : null,
        };
    }
}
