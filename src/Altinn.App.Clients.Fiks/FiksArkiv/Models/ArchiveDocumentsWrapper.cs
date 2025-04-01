using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

internal sealed record ArchiveDocumentsWrapper
{
    public MessagePayloadWrapper PrimaryDocument { get; }
    public IEnumerable<MessagePayloadWrapper> AttachmentDocuments { get; }
    private List<MessagePayloadWrapper> _allDocuments { get; }

    public ArchiveDocumentsWrapper(
        MessagePayloadWrapper primaryDocument,
        IEnumerable<MessagePayloadWrapper> attachmentDocuments
    )
    {
        PrimaryDocument = primaryDocument;
        AttachmentDocuments = attachmentDocuments;
        _allDocuments = [PrimaryDocument, .. AttachmentDocuments];

        _allDocuments.EnsureUniqueFilenames();
    }

    public IEnumerable<FiksIOMessagePayload> ToPayloads() => _allDocuments.Select(x => x.Payload);
}
