using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

internal sealed record MessagePayloadWrapper(FiksIOMessagePayload Payload, Kode FileTypeCode, string? FileFormatCode)
{
    public Format GetFileFormat() =>
        new()
        {
            KodeProperty = !string.IsNullOrWhiteSpace(FileFormatCode)
                ? FileFormatCode
                : Payload.GetDotlessFileExtension(),
        };
}
