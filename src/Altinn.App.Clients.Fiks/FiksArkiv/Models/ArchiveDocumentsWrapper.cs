using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

internal sealed record ArchiveDocumentsWrapper(
    MessagePayloadWrapper FormDocument,
    List<MessagePayloadWrapper> Attachments
)
{
    private IEnumerable<MessagePayloadWrapper> GetAllDocuments() => [FormDocument, .. Attachments];

    public IEnumerable<FiksIOMessagePayload> ToPayloads() => GetAllDocuments().Select(x => x.Payload);

    public ArchiveDocumentsWrapper EnsureUniqueFilenames()
    {
        GetAllDocuments().EnsureUniqueFilenames();
        return this;
    }
}
