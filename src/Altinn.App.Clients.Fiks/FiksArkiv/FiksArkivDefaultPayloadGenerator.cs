using System.Text;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.Factories;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultPayloadGenerator : IFiksArkivPayloadGenerator
{
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly ILogger<FiksArkivDefaultPayloadGenerator> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IFiksArkivConfigResolver _fiksArkivConfigResolver;
    private readonly FiksIOSettings _fiksIOSettings;
    private readonly TimeProvider _timeProvider;

    public FiksArkivDefaultPayloadGenerator(
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IAuthenticationContext authenticationContext,
        ILogger<FiksArkivDefaultPayloadGenerator> logger,
        IHostEnvironment hostEnvironment,
        IFiksArkivConfigResolver fiksArkivConfigResolver,
        IOptions<FiksIOSettings> fiksIOSettings,
        TimeProvider? timeProvider = null
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _authenticationContext = authenticationContext;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _fiksArkivConfigResolver = fiksArkivConfigResolver;
        _fiksIOSettings = fiksIOSettings.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FiksIOMessagePayload>> GeneratePayload(
        string taskId,
        Instance instance,
        FiksArkivRecipient recipient
    )
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var documentCreator = appMetadata.AppIdentifier.Org;
        var archiveDocuments = await GetArchiveDocuments(instance);
        var defaultDocumentTitle = await _fiksArkivConfigResolver.GetApplicationTitle();
        var documentMetadata = await _fiksArkivConfigResolver.GetConfigMetadata(instance);
        var recipientDetails = _fiksArkivConfigResolver.GetRecipientParty(instance, recipient);
        var serviceOwnerDetails = await _fiksArkivConfigResolver.GetServiceOwnerParty();
        var instanceOwnerDetails = await _fiksArkivConfigResolver.GetInstanceOwnerParty(instance);
        var submitterDetails = await _fiksArkivConfigResolver.GetFormSubmitterClassification(
            _authenticationContext.Current
        );

        var caseFile = new Saksmappe
        {
            Tittel = documentMetadata?.CaseFileTitle ?? defaultDocumentTitle,
            OffentligTittel = documentMetadata?.CaseFileTitle ?? defaultDocumentTitle,
            AdministrativEnhet = new AdministrativEnhet { Navn = documentCreator },
            Saksaar = _timeProvider.GetLocalNow().Year,
            Saksdato = _timeProvider.GetLocalNow().DateTime,
            ReferanseEksternNoekkel = new EksternNoekkel
            {
                Fagsystem = appMetadata.AppIdentifier.ToString(),
                Noekkel = documentMetadata?.CaseFileId ?? instance.Id,
            },
        };

        caseFile.Klassifikasjon.Add(submitterDetails);

        var journalEntry = new Journalpost
        {
            Journalaar = _timeProvider.GetLocalNow().Year,
            DokumentetsDato = _timeProvider.GetLocalNow().DateTime,
            SendtDato = _timeProvider.GetLocalNow().DateTime,
            Tittel = documentMetadata?.JournalEntryTitle ?? defaultDocumentTitle,
            OffentligTittel = documentMetadata?.JournalEntryTitle ?? defaultDocumentTitle,
            OpprettetAv = documentCreator,
            ArkivertAv = documentCreator,
            Journalstatus = new Journalstatus
            {
                KodeProperty = JournalstatusKoder.Journalfoert.Verdi,
                Beskrivelse = JournalstatusKoder.Journalfoert.Beskrivelse,
            },
            Journalposttype = new Journalposttype
            {
                KodeProperty = JournalposttypeKoder.InngaaendeDokument.Verdi,
                Beskrivelse = JournalposttypeKoder.InngaaendeDokument.Beskrivelse,
            },
            ReferanseForelderMappe = new ReferanseTilMappe
            {
                ReferanseEksternNoekkel = caseFile.ReferanseEksternNoekkel,
            },
            ReferanseEksternNoekkel = caseFile.ReferanseEksternNoekkel,
        };

        // Recipient
        journalEntry.Korrespondansepart.Add(recipientDetails);

        // Sender(s)
        journalEntry.Korrespondansepart.Add(serviceOwnerDetails);
        if (instanceOwnerDetails is not null)
        {
            journalEntry.Korrespondansepart.Add(instanceOwnerDetails);
        }

        // Internal sender
        journalEntry.Korrespondansepart.Add(
            KorrespondansepartFactory.CreateInternalSender(
                partyName: FiksArkivConstants.AltinnSystemId,
                partyId: FiksArkivConstants.AltinnOrgNo
            )
        );

        // Main form data file
        journalEntry.Dokumentbeskrivelse.Add(GetDocumentMetadata(archiveDocuments.PrimaryDocument));

        // Attachments
        foreach (var attachment in archiveDocuments.AttachmentDocuments)
        {
            journalEntry.Dokumentbeskrivelse.Add(GetDocumentMetadata(attachment));
        }

        // Archive record
        var archiveRecord = new Arkivmelding
        {
            Mappe = caseFile,
            Registrering = journalEntry,
            AntallFiler = journalEntry.Dokumentbeskrivelse.Count,
            System = documentMetadata?.SystemId ?? FiksArkivConstants.AltinnSystemId,
            Regel = documentMetadata?.RuleId,
        };

        if (!_hostEnvironment.IsProduction())
        {
            string xmlResult = Encoding.UTF8.GetString(archiveRecord.SerializeXmlBytes(indent: true).Span);
            _logger.LogInformation(xmlResult);
        }

        return [archiveRecord.ToPayload(), .. archiveDocuments.ToPayloads()];
    }

    private async Task<FiksArkivDocuments> GetArchiveDocuments(Instance instance)
    {
        InstanceIdentifier instanceId = new(instance.Id);
        var primaryDocumentSettings = _fiksArkivConfigResolver.PrimaryDocumentSettings;
        var primaryDataElement = instance.GetRequiredDataElement(primaryDocumentSettings.DataType);
        var primaryDocument = await GetPayload(
            primaryDataElement,
            primaryDocumentSettings.Filename,
            DokumenttypeKoder.Dokument,
            instanceId
        );

        List<MessagePayloadWrapper> attachmentDocuments = [];
        foreach (var attachmentSetting in _fiksArkivConfigResolver.AttachmentSettings)
        {
            IReadOnlyList<DataElement> dataElements = instance
                .GetOptionalDataElements(attachmentSetting.DataType)
                .ToList();

            if (dataElements.Any() is false)
                continue;

            attachmentDocuments.AddRange(
                await Task.WhenAll(
                    dataElements.Select(async x =>
                        await GetPayload(x, attachmentSetting.Filename, DokumenttypeKoder.Vedlegg, instanceId)
                    )
                )
            );
        }

        return new FiksArkivDocuments(primaryDocument, attachmentDocuments);
    }

    private async Task<MessagePayloadWrapper> GetPayload(
        DataElement dataElement,
        string? filename,
        Kode fileTypeCode,
        InstanceIdentifier instanceId
    )
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        if (string.IsNullOrWhiteSpace(filename) is false)
            dataElement.Filename = filename;
        else if (string.IsNullOrWhiteSpace(dataElement.Filename))
            dataElement.Filename = $"{dataElement.DataType}{dataElement.GetExtensionForMimeType()}";

        return new MessagePayloadWrapper(
            new FiksIOMessagePayload(
                dataElement.Filename,
                await _dataClient.GetDataBytes(
                    appMetadata.AppIdentifier.Org,
                    appMetadata.AppIdentifier.App,
                    instanceId.InstanceOwnerPartyId,
                    instanceId.InstanceGuid,
                    Guid.Parse(dataElement.Id)
                )
            ),
            fileTypeCode
        );
    }

    private Dokumentbeskrivelse GetDocumentMetadata(MessagePayloadWrapper payloadWrapper)
    {
        var documentClassification =
            payloadWrapper.FileTypeCode == DokumenttypeKoder.Dokument
                ? TilknyttetRegistreringSomKoder.Hoveddokument
                : TilknyttetRegistreringSomKoder.Vedlegg;

        var metadata = new Dokumentbeskrivelse
        {
            Dokumenttype = new Dokumenttype
            {
                KodeProperty = payloadWrapper.FileTypeCode.Verdi,
                Beskrivelse = payloadWrapper.FileTypeCode.Beskrivelse,
            },
            Dokumentstatus = new Dokumentstatus
            {
                KodeProperty = DokumentstatusKoder.Ferdig.Verdi,
                Beskrivelse = DokumentstatusKoder.Ferdig.Beskrivelse,
            },
            Tittel = payloadWrapper.Payload.Filename,
            TilknyttetRegistreringSom = new TilknyttetRegistreringSom
            {
                KodeProperty = documentClassification.Verdi,
                Beskrivelse = documentClassification.Beskrivelse,
            },
            OpprettetDato = _timeProvider.GetLocalNow().DateTime,
        };

        metadata.Dokumentobjekt.Add(
            new Dokumentobjekt
            {
                SystemID = new SystemID
                {
                    Value = _fiksIOSettings.AccountId.ToString(),
                    Label = FiksArkivConstants.AltinnSystemId,
                },
                Filnavn = payloadWrapper.Payload.Filename,
                ReferanseDokumentfil = payloadWrapper.Payload.Filename,
                Format = new Format { KodeProperty = payloadWrapper.Payload.GetDotlessFileExtension() },
                Variantformat = new Variantformat
                {
                    KodeProperty = VariantformatKoder.Produksjonsformat.Verdi,
                    Beskrivelse = VariantformatKoder.Produksjonsformat.Beskrivelse,
                },
            }
        );

        return metadata;
    }
}
