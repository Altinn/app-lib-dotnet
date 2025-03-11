using System.Globalization;
using System.Text;
using Altinn.App.Clients.Fiks.Extensions;
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
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultMessageProvider : IFiksArkivMessageProvider
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly FiksIOSettings _fiksIOSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly ILogger<FiksArkivDefaultMessageProvider> _logger;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly IAltinnCdnClient _altinnCdnClient;

    private ApplicationMetadata? _applicationMetadataCache;

    public FiksArkivDefaultMessageProvider(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IOptions<FiksIOSettings> fiksIOSettings,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IAuthenticationContext authenticationContext,
        IAltinnPartyClient altinnPartyClient,
        ILogger<FiksArkivDefaultMessageProvider> logger,
        IAltinnCdnClient altinnCdnClient
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _altinnCdnClient = altinnCdnClient;
        _altinnPartyClient = altinnPartyClient;
        _authenticationContext = authenticationContext;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksIOSettings = fiksIOSettings.Value;
        _logger = logger;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        if (IsEnabledForTask(taskId) is false)
            throw new Exception($"Fiks Arkiv error: Auto-send is not enabled for this task: {taskId}");

        var recipient = await GetRecipient(instance);
        var instanceId = new InstanceIdentifier(instance.Id);
        var messagePayloads = await GenerateMessagePayloads(instance, recipient);

        return new FiksIOMessageRequest(
            Recipient: recipient,
            MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(2),
            // Payload: messagePayloads
            Payload: messagePayloads.Take(1) // TEMP: No attachments for now
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

    private bool IsEnabledForTask(string taskId) => _fiksArkivSettings.AutoSend?.AfterTaskId == taskId;

    private async Task<ApplicationMetadata> GetApplicationMetadata()
    {
        _applicationMetadataCache ??= await _appMetadata.GetApplicationMetadata();
        return _applicationMetadataCache;
    }

    private static T VerifiedNotNull<T>(T? value)
    {
        if (value is null)
            throw new Exception($"Value of type {typeof(T).Name} is unexpectedly null.");

        return value;
    }

    private async Task<IEnumerable<FiksIOMessagePayload>> GenerateMessagePayloads(Instance instance, Guid recipient)
    {
        var appMetadata = await GetApplicationMetadata();
        var documentTitle = appMetadata.Title[LanguageConst.Nb];
        var documentCreator = appMetadata.AppIdentifier.Org;
        var recipientDetails = GetRecipientParty(instance, recipient);
        var serviceOwnerDetails = await GetServiceOwnerParty();
        var instanceOwnerDetails = await GetInstanceOwnerParty(instance);
        var submitterDetails = await GetFormSubmitterClassification();
        var documents = await GetDocuments(instance);

        var caseFile = new Saksmappe
        {
            Tittel = documentTitle,
            OffentligTittel = documentTitle,
            AdministrativEnhet = new AdministrativEnhet { Navn = documentCreator },
            Saksaar = DateTime.Now.Year,
            Saksdato = DateTime.Now,
            ReferanseEksternNoekkel = new EksternNoekkel
            {
                Fagsystem = appMetadata.AppIdentifier.ToString(), // TODO: What should this value really be?
                Noekkel = instance.Id,
            },
        };

        caseFile.Klassifikasjon.Add(submitterDetails);

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
                KorrespondansepartNavn = FiksArkivConstants.AltinnSystemLabel,
                KorrespondansepartID = FiksArkivConstants.AltinnOrgNo,
            }
        );

        // Main form data file
        journalEntry.Dokumentbeskrivelse.Add(GetDocumentMetadata(documents.FormDocument));

        // Attachments
        foreach (var attachment in documents.Attachments)
        {
            journalEntry.Dokumentbeskrivelse.Add(GetDocumentMetadata(attachment));
        }

        // Archive record
        var archiveRecord = new Arkivmelding
        {
            Mappe = caseFile,
            Registrering = journalEntry,
            AntallFiler = journalEntry.Dokumentbeskrivelse.Count + 1,
            System = FiksArkivConstants.AltinnSystemLabel,
        };

        // TEMP log
        string xmlResult = Encoding.UTF8.GetString(archiveRecord.SerializeXmlBytes(indent: true).Span);
        _logger.LogWarning(xmlResult);

        return
        [
            archiveRecord.ToPayload(),
            documents.FormDocument.Payload,
            .. documents.Attachments.Select(x => x.Payload),
        ];
    }

    private async Task<ArchiveDocuments> GetDocuments(Instance instance)
    {
        InstanceIdentifier instanceId = new(instance.Id);
        var formDocumentSettings = VerifiedNotNull(_fiksArkivSettings.AutoSend?.FormDocument);
        var attachmentSettings = _fiksArkivSettings.AutoSend?.Attachments ?? [];

        var formElement = instance.Data.First(x => x.DataType == formDocumentSettings.DataType);
        var formDocument = await GetPayload(
            formElement,
            formDocumentSettings.Filename,
            DokumenttypeKoder.Dokument,
            instanceId
        );

        List<PayloadWrapper> attachments = [];
        foreach (var attachmentSetting in attachmentSettings)
        {
            List<DataElement> dataElements = instance
                .Data.Where(x => x.DataType == attachmentSetting.DataType)
                .ToList();

            if (dataElements.Count == 0)
                throw new Exception(
                    $"Fiks Arkiv error: No data elements found for Attachment.DataType '{attachmentSetting.DataType}'"
                );

            attachments.AddRange(
                await Task.WhenAll(
                    dataElements.Select(async x =>
                        await GetPayload(x, attachmentSetting.Filename, DokumenttypeKoder.Vedlegg, instanceId)
                    )
                )
            );
        }

        // TODO: This specifically requires testing
        EnsureUniqueFilenames([formDocument, .. attachments]);

        return new ArchiveDocuments(formDocument, attachments);
    }

    private async Task<PayloadWrapper> GetPayload(
        DataElement dataElement,
        string? filename,
        Kode fileTypeCode,
        InstanceIdentifier instanceId
    )
    {
        ApplicationMetadata appMetadata = await GetApplicationMetadata();

        if (string.IsNullOrWhiteSpace(filename) is false)
        {
            dataElement.Filename = filename;
        }
        else if (string.IsNullOrWhiteSpace(dataElement.Filename))
        {
            dataElement.Filename = $"{dataElement.DataType}{dataElement.GetExtensionForMimeType()}";
        }

        return new PayloadWrapper(
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

    private static void EnsureUniqueFilenames(List<PayloadWrapper> attachments)
    {
        var hasDuplicateFilenames = attachments
            .GroupBy(x => x.Payload.Filename.ToLowerInvariant())
            .Where(x => x.Count() > 1)
            .Select(x => x.ToList());

        foreach (var duplicates in hasDuplicateFilenames)
        {
            for (int i = 0; i < duplicates.Count; i++)
            {
                int uniqueId = i + 1;
                string filename = Path.GetFileNameWithoutExtension(duplicates[i].Payload.Filename);
                string extension = Path.GetExtension(duplicates[i].Payload.Filename);

                duplicates[i] = duplicates[i] with
                {
                    Payload = duplicates[i].Payload with { Filename = $"{filename}({uniqueId}){extension}" },
                };
            }
        }
    }

    private Task<Guid> GetRecipient(Instance instance)
    {
        var configuredRecipient = VerifiedNotNull(_fiksArkivSettings.AutoSend?.Recipient);

        // TODO: dynamic dot-pick query with instance data here...?
        var recipient = Guid.Parse(configuredRecipient);

        return Task.FromResult(recipient);
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

    private static Korrespondansepart GetRecipientParty(Instance instance, Guid recipient)
    {
        return new Korrespondansepart
        {
            KorrespondansepartID = recipient.ToString(), // TODO: Consider using `kommunenummer` or similar here
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

    private async Task<Korrespondansepart> GetServiceOwnerParty()
    {
        ApplicationMetadata appMetadata = await GetApplicationMetadata();
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

        return new Korrespondansepart
        {
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Avsender.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Avsender.Beskrivelse,
            },
            KorrespondansepartNavn =
                orgDetails?.Name?.Nb ?? orgDetails?.Name?.Nn ?? orgDetails?.Name?.En ?? appMetadata.Org,
            KorrespondansepartID = orgDetails?.Orgnr ?? appMetadata.Org,
        };
    }

    private async Task<Klassifikasjon> GetFormSubmitterClassification()
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

    private async Task<Korrespondansepart?> GetInstanceOwnerParty(Instance instance)
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
                KorrespondansepartNavn = party.Name,
                KorrespondansepartID =
                    party.PartyUuid?.ToString() ?? party.PartyId.ToString(CultureInfo.InvariantCulture),
            };

            if (party.Organization is not null)
            {
                correspondencePart.Telefonnummer.Add(party.Organization.MobileNumber);
                correspondencePart.Telefonnummer.Add(party.Organization.TelephoneNumber);
                correspondencePart.Postadresse.Add(party.Organization.MailingAddress);
                correspondencePart.Postnummer = party.Organization.MailingPostalCode;
                correspondencePart.Poststed = party.Organization.MailingPostalCity;
            }
            else if (party.Person is not null)
            {
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

    private Dokumentbeskrivelse GetDocumentMetadata(PayloadWrapper payloadWrapper)
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
            OpprettetDato = DateTime.Now,
        };

        metadata.Dokumentobjekt.Add(
            new Dokumentobjekt
            {
                SystemID = new SystemID
                {
                    Value = _fiksIOSettings.AccountId.ToString(),
                    Label = FiksArkivConstants.AltinnSystemLabel,
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

    private sealed record ArchiveDocuments(PayloadWrapper FormDocument, List<PayloadWrapper> Attachments);

    private sealed record PayloadWrapper(FiksIOMessagePayload Payload, Kode FileTypeCode);
}
