using System.Text;
using System.Xml.Serialization;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1;
using KS.Fiks.Arkiv.Forenklet.Arkivering.V1.Helpers;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using KS.Fiks.IO.Crypto.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kode = KS.Fiks.IO.Arkiv.Client.ForenkletArkivering.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultMessageProvider : IFiksArkivMessageProvider
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly ILogger<FiksArkivDefaultMessageProvider> _logger;

    public FiksArkivDefaultMessageProvider(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        ILogger<FiksArkivDefaultMessageProvider> logger
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _logger = logger;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        var recipient = await GetRecipient(instance);
        var documents = await GetDocuments(instance);
        var instanceId = new InstanceIdentifier(instance.Id);

        // TODO: Build arkivmelding.xml
        var arkivMelding = await GenerateArkivmelding(documents);

        return new FiksIOMessageRequest(
            Recipient: recipient,
            MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(14),
            Payload: [arkivMelding, documents.FormDocument, .. documents.Attachments]
        );
    }

    public void ValidateConfiguration()
    {
        if (_fiksArkivSettings.AutoSend is null)
            return;

        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend.Recipient))
            throw new Exception("Fiks Arkiv error: Recipient configuration is required for auto-send.");

        if (
            _fiksArkivSettings.AutoSend.Recipient.DoesNotContain('-')
            && _fiksArkivSettings.AutoSend.Recipient.DoesNotContain('.')
        )
            throw new Exception("Fiks Arkiv error: Recipient must be a valid Guid or a data model path.");

        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend.AfterTaskId))
            throw new Exception("Fiks Arkiv error: AfterTaskId configuration is required for auto-send.");

        if (
            _fiksArkivSettings.AutoSend.FormDocument is null
            || string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend.FormDocument.DataType)
        )
            throw new Exception("Fiks Arkiv error: FormDocument configuration is required for auto-send.");

        if (_fiksArkivSettings.AutoSend.Attachments?.Any(x => string.IsNullOrWhiteSpace(x.DataType)) is true)
            throw new Exception("Fiks Arkiv error: Attachments must have DataType set for all entries.");
    }

    private async Task<ArkivDocuments> GetDocuments(Instance instance)
    {
        var instanceId = new InstanceIdentifier(instance.Id);
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var formDocumentSettings = _fiksArkivSettings.AutoSend?.FormDocument;
        var attachmentSettings = _fiksArkivSettings.AutoSend?.Attachments ?? [];

        // This should already have been validated by ValidateConfiguration
        if (string.IsNullOrWhiteSpace(formDocumentSettings?.DataType))
            throw new Exception($"Fiks Arkiv error: Invalid FormDocument configuration");

        var formElement = instance.Data.First(x => x.DataType == formDocumentSettings.DataType);
        var formDocument = await GetPayload(formElement, formDocumentSettings, instanceId, appMetadata);

        List<FiksIOMessagePayload> attachments = [];
        foreach (var attachmentSetting in attachmentSettings)
        {
            // This should already have been validated by ValidateConfiguration
            if (string.IsNullOrWhiteSpace(attachmentSetting.DataType))
                throw new Exception($"Fiks Arkiv error: Invalid Attachment configuration '{attachmentSetting}'");

            List<DataElement> dataElements = instance
                .Data.Where(x => x.DataType == attachmentSetting.DataType)
                .ToList();
            if (dataElements.Count == 0)
                throw new Exception(
                    $"Fiks Arkiv error: No data elements found for Attachment.DataType '{attachmentSetting.DataType}'"
                );

            attachments.AddRange(
                await Task.WhenAll(
                    dataElements.Select(async x => await GetPayload(x, attachmentSetting, instanceId, appMetadata))
                )
            );
        }

        // TODO: This specifically requires testing
        EnsureUniqueFilenames([formDocument, ..attachments]);

        return new ArkivDocuments(formDocument, attachments);
    }

    private async Task<FiksIOMessagePayload> GetPayload(
        DataElement dataElement,
        FiksArkivPayloadSettings payloadSettings,
        InstanceIdentifier instanceId,
        ApplicationMetadata appMetadata
    )
    {
        if (string.IsNullOrWhiteSpace(payloadSettings.Filename) is false)
        {
            dataElement.Filename = payloadSettings.Filename;
        }
        else if (string.IsNullOrWhiteSpace(dataElement.Filename))
        {
            var extension = GetExtensionForMimeType(dataElement.ContentType);
            dataElement.Filename = $"{dataElement.DataType}{extension}";
        }

        return new FiksIOMessagePayload(
            dataElement.Filename,
            await _dataClient.GetDataBytes(
                appMetadata.AppIdentifier.Org,
                appMetadata.AppIdentifier.App,
                instanceId.InstanceOwnerPartyId,
                instanceId.InstanceGuid,
                Guid.Parse(dataElement.Id)
            )
        );
    }

    private static List<FiksIOMessagePayload> EnsureUniqueFilenames(List<FiksIOMessagePayload> attachments)
    {
        var hasDuplicateFilenames = attachments
            .GroupBy(x => x.Filename.ToLowerInvariant())
            .Where(x => x.Count() > 1)
            .Select(x => x.ToList());

        foreach (var duplicates in hasDuplicateFilenames)
        {
            for (int i = 0; i < duplicates.Count; i++)
            {
                int uniqueId = i + 1;
                string filename = Path.GetFileNameWithoutExtension(duplicates[i].Filename);
                string extension = Path.GetExtension(duplicates[i].Filename);

                duplicates[i] = duplicates[i] with { Filename = $"{filename}({uniqueId}){extension}" };
            }
        }

        return attachments;
    }

    private Task<Guid> GetRecipient(Instance instance)
    {
        // This should already have been validated by ValidateConfiguration
        if (_fiksArkivSettings.AutoSend?.Recipient is null)
            throw new Exception("Fiks Arkiv error: Recipient is missing from AutoSend settings.");

        // TODO: dynamic dot-pick query with instance data here...?
        var recipient = Guid.Parse(_fiksArkivSettings.AutoSend.Recipient);

        return Task.FromResult(recipient);
    }

    private static string? GetExtensionForMimeType(string? mimeType)
    {
        if (mimeType is null)
            return null;

        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/xml"] = ".xml",
            ["text/xml"] = ".xml",
            ["application/pdf"] = ".pdf",
            ["application/json"] = ".json",
        };

        return mapping.GetValueOrDefault(mimeType);
    }

    // private async Task Test()
    // {
    //     ILayoutEvaluatorStateInitializer _layoutStateInit;
    //     IInstanceClient _instanceClient;
    //     Instance instance;
    //     ModelSerializationService _modelSerialization;
    //
    //     IInstanceDataAccessor dataAccessor = new InstanceDataUnitOfWork(
    //         instance,
    //         _dataClient,
    //         _instanceClient,
    //         await _appMetadata.GetApplicationMetadata(),
    //         _modelSerialization
    //     );
    //
    //     LayoutEvaluatorState state = await _layoutStateInit.Init(
    //         dataAccessor,
    //         taskId: null // don't load layout for task
    //     );
    // }

    private async Task<FiksIOMessagePayload> GenerateArkivmelding(ArkivDocuments documents)
    {
        var factory = new ArkivmeldingFactory();
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var documentTitle = appMetadata.Title[LanguageConst.Nb];
        var documentCreator = appMetadata.AppIdentifier.Org;

        var referanseSaksmappeForenklet = new SaksmappeForenklet
        {
            saksaar = DateTime.Now.Year,
            // sakssekvensnummer = 0,
            // mappetype = new Kode { kodeverdi = "saksmappe", kodebeskrivelse = "..." },
            saksdato = DateTime.UtcNow,
            tittel = documentTitle,
            offentligTittel = documentTitle,
            administrativEnhet = documentCreator,
            // referanseAdministrativEnhet = "ref",
            saksansvarlig = "Ingen",
            // referanseSaksansvarlig = "ref",
            // saksstatus = "status",
            // avsluttetAv = "avsluttet",
            // skjermetTittel = false,
            referanseEksternNoekkelForenklet = new EksternNoekkelForenklet
            {
                fagsystem = "SaksOgArkivsystem", // TODO: What should this value really be?
                noekkel = Guid.NewGuid().ToString(),
            },
            klasse =
            [
                new KlasseForenklet
                {
                    klasseID = <PERSONNUMMER>,
                    klassifikasjonssystem = "Fødselsnummer",
                    // skjermetKlasse = false,
                    tittel = <NAVNPAAPERSON>,
                },
            ],
        };

        var nyUtgaaendeJournalpost = new UtgaaendeJournalpost
        {
            journalaar = DateTime.Now.Year,
            // journalsekvensnummer = 0,
            // journalpostnummer = 0,
            dokumentetsDato = DateTime.Now,
            sendtDato = DateTime.Now,
            hoveddokument = documents.FormDocument.ToForenkletDokument(DokumenttypeKoder.Dokument),
            // skjermetTittel = false,
            tittel = documentTitle,
            offentligTittel = documentTitle,
            // referanseEksternNoekkelForenklet = new EksternNoekkelForenklet
            // {
            //     fagsystem = "fagsystem",
            //     noekkel = "noekkel",
            // },
            vedlegg = documents.Attachments.Select(x => x.ToForenkletDokument(DokumenttypeKoder.Vedlegg)).ToList(),
            mottaker =
            [
                new KorrespondansepartForenklet
                {
                    systemID = "id",
                    enhetsidentifikator = new Enhetsidentifikator { organisasjonsnummer = "orgnr", landkode = "kode" },
                    personid = new Personidentifikator
                    {
                        personidentifikatorNr = "fnr",
                        personidentifikatorLandkode = "kode",
                    },
                    korrespondanseparttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                    navn = "navn",
                    // skjermetKorrespondansepart = false,
                    postadresse = null,
                    kontaktinformasjonForenklet = new KontaktinformasjonForenklet
                    {
                        epostadresse = "epost",
                        mobiltelefon = "nummer",
                        telefon = "nummer",
                    },
                    kontaktperson = "person",
                    deresReferanse = "deresreferanse",
                    forsendelsemåte = "måte",
                },
            ],
            avsender =
            [
                new KorrespondansepartForenklet
                {
                    systemID = documentCreator,
                    enhetsidentifikator = new Enhetsidentifikator { organisasjonsnummer = "orgnr", landkode = "NO" },
                    personid = new Personidentifikator
                    {
                        personidentifikatorNr = "fnr",
                        personidentifikatorLandkode = "kode",
                    },
                    korrespondanseparttype = new Kode { kodeverdi = "tja", kodebeskrivelse = "..." },
                    navn = "navn",
                    // skjermetKorrespondansepart = false,
                    postadresse = null,
                    kontaktinformasjonForenklet = new KontaktinformasjonForenklet
                    {
                        epostadresse = "epost",
                        mobiltelefon = "nummer",
                        telefon = "nummer",
                    },
                    kontaktperson = "person",
                    deresReferanse = "deresreferanse",
                    forsendelsemåte = "måte",
                },
            ],
            internAvsender =
            [
                new KorrespondansepartIntern
                {
                    administrativEnhet = "Altinn",
                    // referanseAdministrativEnhet = "ref",
                    // saksbehandler = "behandler",
                    // referanseSaksbehandler = "ref",
                },
            ],
        };

        var forenkletUtgaaende = new ArkivmeldingForenkletUtgaaende
        {
            sluttbrukerIdentifikator = documentCreator,
            referanseSaksmappeForenklet = referanseSaksmappeForenklet,
            nyUtgaaendeJournalpost = nyUtgaaendeJournalpost,
        };

        var arkivmelding = factory.GetArkivmelding(forenkletUtgaaende);
        // StringWriter logWriter = new StringWriter();
        // XmlSerializer logSerializer = new XmlSerializer(typeof(Arkivmelding));
        // logSerializer.Serialize(logWriter, arkivmelding);
        // _logger.LogWarning(logWriter.ToString());

        return new FiksIOMessagePayload("arkivmelding.xml", arkivmelding);
    }

    private record ArkivDocuments(FiksIOMessagePayload FormDocument, List<FiksIOMessagePayload> Attachments);
}
