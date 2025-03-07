using System.Globalization;
using System.Xml.Serialization;
// using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
// using KS.Fiks.Arkiv.Forenklet.Arkivering.V1;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
// using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
// using KS.Fiks.IO.Arkiv.Client.ForenkletArkivering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultMessageProvider : IFiksArkivMessageProvider
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly ILogger<FiksArkivDefaultMessageProvider> _logger;
    private IAltinnCdnClient? _altinnCdnClient;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly FiksIOSettings _fiksIOSettings;

    public FiksArkivDefaultMessageProvider(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IOptions<FiksIOSettings> fiksIoSettings,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IAuthenticationContext authenticationContext,
        IAltinnPartyClient altinnPartyClient,
        ILogger<FiksArkivDefaultMessageProvider> logger,
        IAltinnCdnClient? altinnCdnClient = null
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _altinnCdnClient = altinnCdnClient;
        _altinnPartyClient = altinnPartyClient;
        _authenticationContext = authenticationContext;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksIOSettings = fiksIoSettings.Value;
        _logger = logger;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        var recipient = await GetRecipient(instance);
        var documents = await GetDocuments(instance);
        var instanceId = new InstanceIdentifier(instance.Id);
        var archiveMessage = await GenerateArchiveMessage(instance, documents, recipient);

        return new FiksIOMessageRequest(
            Recipient: recipient,
            // MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            MessageType: "no.ks.fiks.arkiv.v1.arkivering.arkivmelding.opprett",
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(14),
            // Payload: [archiveMessage, documents.FormDocument, .. documents.Attachments]
            Payload: [archiveMessage]
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

    private async Task<ArchiveDocuments> GetDocuments(Instance instance)
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
        EnsureUniqueFilenames([formDocument, .. attachments]);

        return new ArchiveDocuments(formDocument, attachments);
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

    private static void EnsureUniqueFilenames(List<FiksIOMessagePayload> attachments)
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

    private static Korrespondansepart GetRecipientDetails(Instance instance, Guid recipient)
    {
        return new Korrespondansepart
        {
            KorrespondansepartID = recipient.ToString(),
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Mottaker.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Mottaker.Beskrivelse,
            },
            Organisasjonid = "MOTTAKERS-ORGNUMMER",
            KorrespondansepartNavn = "MOTTAKERS-NAVN",
            DeresReferanse = instance.Id,
        };
    }

    private async Task<Korrespondansepart> GetServiceOwnerDetails(
        Instance instance,
        ApplicationMetadata appMetadata,
        string systemId
    )
    {
        bool disposeClient = _altinnCdnClient is null;
        _altinnCdnClient ??= new AltinnCdnClient();

        AltinnCdnOrgDetails? orgDetails = null;

        try
        {
            AltinnCdnOrgs altinnCdnOrgs = await _altinnCdnClient.GetOrgs();
            orgDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to get service owner details: {Exception}", e);
        }
        finally
        {
            if (disposeClient)
            {
                _altinnCdnClient?.Dispose();
                _altinnCdnClient = null;
            }
        }

        return new Korrespondansepart
        {
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Avsender.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Avsender.Beskrivelse,
            },
            Organisasjonid = orgDetails?.Orgnr ?? appMetadata.Org,
            Land = "NO",
            KorrespondansepartNavn =
                orgDetails?.Name?.Nb ?? orgDetails?.Name?.Nn ?? orgDetails?.Name?.En ?? appMetadata.Org,
            KorrespondansepartID = systemId,
            DeresReferanse = instance.Id,
        };
    }

    private async Task<Klassifikasjon> GetSenderDetails()
    {
        switch (_authenticationContext.Current)
        {
            case Authenticated.User user:
            {
                UserProfile userProfile = await user.LookupProfile();

                return new Klassifikasjon
                {
                    KlasseID = userProfile.Party.SSN.ToString(CultureInfo.InvariantCulture),
                    KlassifikasjonssystemID = "Fødselsnummer",
                    Tittel = userProfile.Party.Name,
                };
            }
            case Authenticated.SelfIdentifiedUser selfIdentifiedUser:
                return new Klassifikasjon
                {
                    KlasseID = selfIdentifiedUser.UserId.ToString(CultureInfo.InvariantCulture),
                    KlassifikasjonssystemID = "AltinnBrukerId",
                    Tittel = selfIdentifiedUser.Username,
                };
            case Authenticated.SystemUser systemUser:
                return new Klassifikasjon
                {
                    KlasseID = systemUser.SystemUserId[0].ToString(),
                    KlassifikasjonssystemID = "SystembrukerId",
                    Tittel = systemUser.SystemUserOrgNr.Get(OrganisationNumberFormat.Local),
                };
            case Authenticated.Org org:
                return new Klassifikasjon { KlasseID = org.OrgNo, KlassifikasjonssystemID = "Organisasjonsnummer" };
            default:
                throw new Exception("Could not determine sender details");
        }
    }

    private async Task<Korrespondansepart?> GetInstanceOwnerDetails(Instance instance, string systemId)
    {
        try
        {
            int partyId = int.Parse(instance.InstanceOwner.PartyId, CultureInfo.InvariantCulture);
            Party? party = await _altinnPartyClient.GetParty(partyId);

            if (party is null)
                return null;

            var correspondencePart = new Korrespondansepart
            {
                Korrespondanseparttype = new Korrespondanseparttype
                {
                    KodeProperty = KorrespondanseparttypeKoder.Avsender.Verdi,
                    Beskrivelse = KorrespondanseparttypeKoder.Avsender.Beskrivelse,
                },
                // Land = "NO",
                KorrespondansepartNavn = party.Name,
                KorrespondansepartID = systemId,
                DeresReferanse = instance.Id,
            };

            if (party.Organization is not null)
            {
                correspondencePart.Organisasjonid = party.OrgNumber;
                correspondencePart.Telefonnummer.Add(party.Organization.MobileNumber);
                correspondencePart.Telefonnummer.Add(party.Organization.TelephoneNumber);
                correspondencePart.Postadresse.Add(party.Organization.MailingAddress);
                correspondencePart.Postnummer = party.Organization.MailingPostalCode;
                correspondencePart.Poststed = party.Organization.MailingPostalCity;
            }
            else if (party.Person is not null)
            {
                correspondencePart.Personid = party.SSN;
                correspondencePart.Telefonnummer.Add(party.Person.MobileNumber);
                correspondencePart.Telefonnummer.Add(party.Person.TelephoneNumber);
                correspondencePart.Postadresse.Add(party.Person.MailingAddress);
                correspondencePart.Postnummer = party.Person.MailingPostalCode;
                correspondencePart.Poststed = party.Person.MailingPostalCity;
            }

            return correspondencePart;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not get party: {Exception}", e);
        }

        return null;
    }

    private async Task<FiksIOMessagePayload> GenerateArchiveMessage(
        Instance instance,
        ArchiveDocuments documents,
        Guid recipient
    )
    {
        // var factory = new ArkivmeldingFactory();
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var documentTitle = appMetadata.Title[LanguageConst.Nb];
        var documentCreator = appMetadata.AppIdentifier.Org;
        var recipientDetails = GetRecipientDetails(instance, recipient);
        var serviceOwnerDetails = await GetServiceOwnerDetails(instance, appMetadata, documentCreator);
        var instanceOwnerDetails = await GetInstanceOwnerDetails(instance, documentCreator);
        var senderDetails = await GetSenderDetails();

        var caseFile = new Saksmappe()
        {
            Tittel = documentTitle,
            OffentligTittel = documentTitle,
            AdministrativEnhet = new AdministrativEnhet { Navn = documentCreator },
            Saksaar = DateTime.Now.Year,
            Saksdato = DateTime.Now,
            ReferanseEksternNoekkel = new EksternNoekkel
            {
                Fagsystem = "SaksOgArkivsystem", // TODO: What should this value really be?
                Noekkel = Guid.NewGuid().ToString(),
            },
        };

        caseFile.Klassifikasjon.Add(senderDetails);

        var journalEntry = new Journalpost
        {
            Journalaar = DateTime.Now.Year,
            DokumentetsDato = DateTime.Now,
            SendtDato = DateTime.Now,
            Tittel = documentTitle,
            OffentligTittel = documentTitle,
            OpprettetAv = documentCreator,
            ArkivertAv = documentCreator,
            Journalstatus = new Journalstatus
            {
                KodeProperty = JournalstatusKoder.Journalfoert.Verdi,
                Beskrivelse = JournalstatusKoder.Journalfoert.Beskrivelse,
            },
            Journalposttype = new Journalposttype
            {
                KodeProperty = JournalposttypeKoder.UtgaaendeDokument.Verdi,
                Beskrivelse = JournalposttypeKoder.UtgaaendeDokument.Beskrivelse,
            },
            ReferanseForelderMappe = new ReferanseTilMappe()
            {
                ReferanseEksternNoekkel = caseFile.ReferanseEksternNoekkel,
            },
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
            new Korrespondansepart
            {
                Korrespondanseparttype = new Korrespondanseparttype
                {
                    KodeProperty = KorrespondanseparttypeKoder.InternAvsender.Verdi,
                    Beskrivelse = KorrespondanseparttypeKoder.InternAvsender.Beskrivelse,
                },
                AdministrativEnhet = new AdministrativEnhet { Navn = "Altinn" },
                Organisasjonid = "991825827",
                DeresReferanse = instance.Id,
            }
        );

        // Main form data file
        journalEntry.Dokumentbeskrivelse.Add(
            documents.FormDocument.ToDokumentbeskrivelse(DokumenttypeKoder.Dokument, _fiksIOSettings.AccountId)
        );

        // Attachments
        foreach (var attachment in documents.Attachments)
        {
            journalEntry.Dokumentbeskrivelse.Add(
                attachment.ToDokumentbeskrivelse(DokumenttypeKoder.Vedlegg, _fiksIOSettings.AccountId)
            );
        }

        var archiveMessage = new Arkivmelding
        {
            Mappe = caseFile,
            Registrering = journalEntry,
            AntallFiler = journalEntry.Dokumentbeskrivelse.Count + 1,
            System = "Altinn",
        };

        //
        // var message = factory.GetArkivmelding(
        //     new ArkivmeldingForenkletUtgaaende
        //     {
        //         sluttbrukerIdentifikator = documentCreator,
        //         referanseSaksmappeForenklet = caseFile,
        //         nyUtgaaendeJournalpost = journalEntry,
        //     }
        // );

        StringWriter logWriter = new();
        XmlSerializer logSerializer = new(typeof(Arkivmelding));
        logSerializer.Serialize(logWriter, archiveMessage);
        _logger.LogWarning(logWriter.ToString());

        return new FiksIOMessagePayload("arkivmelding.xml", archiveMessage);
    }

    private sealed record ArchiveDocuments(FiksIOMessagePayload FormDocument, List<FiksIOMessagePayload> Attachments);
}
