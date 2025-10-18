using System.Text.Json.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// Represents a receipt from Fiks Arkiv.
/// </summary>
public sealed record FiksArkivReceipt
{
    /// <summary>
    /// The case file receipt.
    /// </summary>
    [JsonPropertyName("caseFileReceipt")]
    public FiksArkivCaseFileReceipt? CaseFileReceipt { get; init; }

    /// <summary>
    /// The journal entry receipt.
    /// </summary>
    [JsonPropertyName("journalEntryReceipt")]
    public FiksArkivJournalEntryReceipt? JournalEntryReceipt { get; init; }

    internal static FiksArkivReceipt Create(
        SaksmappeKvittering? saksmappeKvittering,
        JournalpostKvittering? journalpostKvittering
    )
    {
        return new FiksArkivReceipt
        {
            CaseFileReceipt = FiksArkivCaseFileReceipt.Create(saksmappeKvittering),
            JournalEntryReceipt = FiksArkivJournalEntryReceipt.Create(journalpostKvittering),
        };
    }

    internal static FiksArkivReceipt Empty() => new();
};

/// <summary>
/// Represents a receipt for a case file.
/// </summary>
public sealed record FiksArkivCaseFileReceipt
{
    /// <summary>
    /// The system that created the case file.
    /// </summary>
    [JsonPropertyName("systemId")]
    public FiksArkivSystemDescription? SystemId { get; init; }

    /// <summary>
    /// The ID of the case file.
    /// </summary>
    [JsonPropertyName("folderId")]
    public string? FolderId { get; init; }

    /// <summary>
    /// The date the case file was created.
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; init; }

    /// <summary>
    /// The year of the case file.
    /// </summary>
    [JsonPropertyName("caseFileYear")]
    public int? CaseYear { get; init; }

    /// <summary>
    /// The date of the case file.
    /// </summary>
    [JsonPropertyName("caseFileDate")]
    public DateTime? CaseDate { get; init; }

    /// <summary>
    /// The case sequence number.
    /// </summary>
    [JsonPropertyName("caseSequenceNumber")]
    public int? CaseSequenceNumber { get; init; }

    /// <summary>
    /// The creator of the case file.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; init; }

    internal static FiksArkivCaseFileReceipt? Create(SaksmappeKvittering? saksmappeKvittering)
    {
        return saksmappeKvittering is null
            ? null
            : new FiksArkivCaseFileReceipt
            {
                SystemId = FiksArkivSystemDescription.Create(saksmappeKvittering.SystemID),
                FolderId = saksmappeKvittering.MappeID,
                CreatedDate = saksmappeKvittering.OpprettetDato,
                CaseYear = saksmappeKvittering.Saksaar,
                CaseDate = saksmappeKvittering.Saksdato,
                CaseSequenceNumber = saksmappeKvittering.Sakssekvensnummer,
                CreatedBy = saksmappeKvittering.OpprettetAv,
            };
    }
};

/// <summary>
/// Represents a receipt for a journal entry.
/// </summary>
public sealed record FiksArkivJournalEntryReceipt
{
    /// <summary>
    /// The system that created the journal entry.
    /// </summary>
    [JsonPropertyName("systemId")]
    public FiksArkivSystemDescription? SystemId { get; init; }

    /// <summary>
    /// The ID of the registration.
    /// </summary>
    [JsonPropertyName("registrationId")]
    public string? RegistrationId { get; init; }

    /// <summary>
    /// The date the journal entry was created.
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; init; }

    /// <summary>
    /// The journal year.
    /// </summary>
    [JsonPropertyName("journalYear")]
    public int? JournalYear { get; init; }

    /// <summary>
    /// The journal date.
    /// </summary>
    [JsonPropertyName("journalDate")]
    public DateTime? JournalDate { get; init; }

    /// <summary>
    /// The journal entry number.
    /// </summary>
    [JsonPropertyName("journalEntryNumber")]
    public int? JournalEntryNumber { get; init; }

    /// <summary>
    /// The journal sequence number.
    /// </summary>
    [JsonPropertyName("journalSequenceNumber")]
    public int? JournalSequenceNumber { get; init; }

    /// <summary>
    /// The journal entry type.
    /// </summary>
    [JsonPropertyName("journalEntryType")]
    public FiksArkivCodeDescription? JournalEntryType { get; init; }

    /// <summary>
    /// The journal status.
    /// </summary>
    [JsonPropertyName("journalStatus")]
    public FiksArkivCodeDescription? JournalStatus { get; init; }

    /// <summary>
    /// The creator of the journal entry.
    /// </summary>
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; init; }

    internal static FiksArkivJournalEntryReceipt? Create(JournalpostKvittering? journalpostKvittering)
    {
        return journalpostKvittering is null
            ? null
            : new FiksArkivJournalEntryReceipt
            {
                SystemId = FiksArkivSystemDescription.Create(journalpostKvittering.SystemID),
                RegistrationId = journalpostKvittering.RegistreringsID,
                CreatedDate = journalpostKvittering.OpprettetDato,
                JournalYear = journalpostKvittering.Journalaar,
                JournalDate = NullIfDefault(journalpostKvittering.Journaldato),
                JournalEntryNumber = journalpostKvittering.Journalpostnummer,
                JournalSequenceNumber = journalpostKvittering.Journalsekvensnummer,
                JournalEntryType = FiksArkivCodeDescription.Create(journalpostKvittering.Journalposttype),
                JournalStatus = FiksArkivCodeDescription.Create(journalpostKvittering.Journalstatus),
                CreatedBy = journalpostKvittering.OpprettetAv,
            };
    }

    private static DateTime? NullIfDefault(DateTime dateTime)
    {
        return dateTime == default ? null : dateTime;
    }
};

/// <summary>
/// Represents a description of a system.
/// </summary>
public sealed record FiksArkivSystemDescription
{
    /// <summary>
    /// The ID of the system.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// The label for the system.
    /// </summary>
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    internal static FiksArkivSystemDescription? Create(SystemID systemId)
    {
        try
        {
            return new FiksArkivSystemDescription { Id = Guid.Parse(systemId.Value), Label = systemId.Label };
        }
        catch (Exception)
        {
            return null;
        }
    }
};

/// <summary>
/// Represents a code and associated description.
/// </summary>
public sealed record FiksArkivCodeDescription
{
    /// <summary>
    /// The code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// The description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    internal static FiksArkivCodeDescription? Create(Kode? kode)
    {
        return kode is null
            ? null
            : new FiksArkivCodeDescription { Code = kode.KodeProperty, Description = kode.Beskrivelse };
    }
};
