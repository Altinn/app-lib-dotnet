using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv.Models;

public class FiksArkivReceiptTest
{
    [Fact]
    public void Create_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var saksmappeKvittering = new SaksmappeKvittering
        {
            SystemID = new SystemID { Value = Guid.NewGuid().ToString(), Label = "system-id" },
            MappeID = "mappe-id",
            OpprettetDato = DateTime.Parse("2025-01-01T00:00:00Z"),
            Saksaar = 1234,
            Saksdato = DateTime.Parse("2025-01-02T00:00:00Z"),
            Sakssekvensnummer = 5678,
            OpprettetAv = "opprettet-av-user",
        };
        var journalpostKvittering = new JournalpostKvittering
        {
            SystemID = new SystemID { Value = Guid.NewGuid().ToString(), Label = "system-id" },
            RegistreringsID = "registrerings-id",
            OpprettetDato = DateTime.Parse("2025-01-03T00:00:00Z"),
            Journalaar = 1001,
            Journaldato = DateTime.Parse("2025-01-04T00:00:00Z"),
            Journalpostnummer = 4,
            Journalsekvensnummer = 903,
            Journalposttype = new Journalposttype { KodeProperty = "A", Beskrivelse = "B" },
            Journalstatus = new Journalstatus { KodeProperty = "C", Beskrivelse = "D" },
            OpprettetAv = "opprettet-av-user",
        };

        // Act
        var result = FiksArkivReceipt.Create(saksmappeKvittering, journalpostKvittering);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CaseFileReceipt);
        Assert.NotNull(result.JournalEntryReceipt);
        Assert.Equal(saksmappeKvittering.SystemID.Label, result.CaseFileReceipt.SystemId!.Label);
        Assert.Equal(saksmappeKvittering.SystemID.Value, result.CaseFileReceipt.SystemId!.Id.ToString());
        Assert.Equal(saksmappeKvittering.MappeID, result.CaseFileReceipt.FolderId);
        Assert.Equal(saksmappeKvittering.OpprettetDato, result.CaseFileReceipt.CreatedDate);
        Assert.Equal(saksmappeKvittering.Saksaar, result.CaseFileReceipt.CaseYear);
        Assert.Equal(saksmappeKvittering.Saksdato, result.CaseFileReceipt.CaseDate);
        Assert.Equal(saksmappeKvittering.Sakssekvensnummer, result.CaseFileReceipt.CaseSequenceNumber);
        Assert.Equal(saksmappeKvittering.OpprettetAv, result.CaseFileReceipt.CreatedBy);
        Assert.Equal(journalpostKvittering.SystemID.Label, result.JournalEntryReceipt!.SystemId!.Label);
        Assert.Equal(journalpostKvittering.SystemID.Value, result.JournalEntryReceipt.SystemId!.Id.ToString());
        Assert.Equal(journalpostKvittering.RegistreringsID, result.JournalEntryReceipt.RegistrationId);
        Assert.Equal(journalpostKvittering.OpprettetDato, result.JournalEntryReceipt.CreatedDate);
        Assert.Equal(journalpostKvittering.Journalaar, result.JournalEntryReceipt.JournalYear);
        Assert.Equal(journalpostKvittering.Journaldato, result.JournalEntryReceipt.JournalDate);
        Assert.Equal(journalpostKvittering.Journalpostnummer, result.JournalEntryReceipt.JournalEntryNumber);
        Assert.Equal(journalpostKvittering.Journalsekvensnummer, result.JournalEntryReceipt.JournalSequenceNumber);
        Assert.Equal(
            journalpostKvittering.Journalposttype.KodeProperty,
            result.JournalEntryReceipt.JournalEntryType!.Code
        );
        Assert.Equal(
            journalpostKvittering.Journalposttype.Beskrivelse,
            result.JournalEntryReceipt.JournalEntryType!.Description
        );
        Assert.Equal(journalpostKvittering.Journalstatus.KodeProperty, result.JournalEntryReceipt.JournalStatus!.Code);
        Assert.Equal(
            journalpostKvittering.Journalstatus.Beskrivelse,
            result.JournalEntryReceipt.JournalStatus!.Description
        );
        Assert.Equal(journalpostKvittering.OpprettetAv, result.JournalEntryReceipt.CreatedBy);
    }
}
