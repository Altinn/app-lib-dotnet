namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

public sealed record FiksArkivDocumentMetadata(
    string? SystemId,
    string? RuleId,
    string? CaseFileId,
    string? CaseFileTitle,
    string? JournalEntryTitle
);
