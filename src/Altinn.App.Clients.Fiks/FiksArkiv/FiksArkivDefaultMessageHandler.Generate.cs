using System.Globalization;
using System.Text;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Kodelister;
using KS.Fiks.Arkiv.Models.V1.Metadatakatalog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Kode = KS.Fiks.Arkiv.Models.V1.Kodelister.Kode;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed partial class FiksArkivDefaultMessageHandler
{
    private async Task<IReadOnlyList<FiksIOMessagePayload>> GenerateMessagePayloads(
        Instance instance,
        RecipientWrapper recipient
    )
    {
        var appMetadata = await GetApplicationMetadata();
        var documentTitle = appMetadata.Title.GetValueOrDefault(LanguageConst.Nb, appMetadata.AppIdentifier.App);
        var documentCreator = appMetadata.AppIdentifier.Org;
        var recipientDetails = GetRecipientParty(instance, recipient);
        var serviceOwnerDetails = await GetServiceOwnerParty();
        var instanceOwnerDetails = await GetInstanceOwnerParty(instance);
        var submitterDetails = await GetFormSubmitterClassification();
        var archiveDocuments = await GetArchiveDocuments(instance);

        var caseFile = new Saksmappe
        {
            Tittel = documentTitle,
            OffentligTittel = documentTitle,
            AdministrativEnhet = new AdministrativEnhet { Navn = documentCreator },
            Saksaar = DateTime.Now.Year,
            Saksdato = DateTime.Now,
            ReferanseEksternNoekkel = new EksternNoekkel
            {
                Fagsystem = appMetadata.AppIdentifier.ToString(),
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
            System = FiksArkivConstants.AltinnSystemLabel,
        };

        if (_hostEnvironment.IsDevelopment())
        {
            string xmlResult = Encoding.UTF8.GetString(archiveRecord.SerializeXmlBytes(indent: true).Span);
            _logger.LogInformation(xmlResult);
        }

        return [archiveRecord.ToPayload(), .. archiveDocuments.ToPayloads()];
    }

    private async Task<ArchiveDocumentsWrapper> GetArchiveDocuments(Instance instance)
    {
        InstanceIdentifier instanceId = new(instance.Id);
        var primaryDocumentSettings = VerifiedNotNull(_fiksArkivSettings.AutoSend?.PrimaryDocument);
        var attachmentSettings = _fiksArkivSettings.AutoSend?.Attachments ?? [];

        var primaryDataElement = instance.GetRequiredDataElement(primaryDocumentSettings.DataType);
        var primaryDocument = await GetPayload(
            primaryDataElement,
            primaryDocumentSettings.Filename,
            DokumenttypeKoder.Dokument,
            instanceId
        );

        List<MessagePayloadWrapper> attachmentDocuments = [];
        foreach (var attachmentSetting in attachmentSettings)
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

        return new ArchiveDocumentsWrapper(primaryDocument, attachmentDocuments);
    }

    private async Task<MessagePayloadWrapper> GetPayload(
        DataElement dataElement,
        string? filename,
        Kode fileTypeCode,
        InstanceIdentifier instanceId
    )
    {
        ApplicationMetadata appMetadata = await GetApplicationMetadata();

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

    private async Task<RecipientWrapper> GetRecipient(Instance instance)
    {
        try
        {
            var recipientSettings = VerifiedNotNull(_fiksArkivSettings.AutoSend?.Recipient);
            var unitOfWork = await _instanceDataUnitOfWorkInitializer.Init(instance, null, null);
            var layoutState = await _layoutStateInitializer.Init(unitOfWork, null);

            return new RecipientWrapper(
                await GetRequiredAccount(layoutState, recipientSettings.FiksAccount),
                await GetOptionalValue(layoutState, recipientSettings.Identifier),
                await GetOptionalValue(layoutState, recipientSettings.OrganizationNumber),
                await GetOptionalValue(layoutState, recipientSettings.Name)
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv error: {Error}", e.Message);
            throw;
        }

        async Task<Guid> GetRequiredAccount(
            LayoutEvaluatorState layoutState,
            FiksArkivRecipientValue<Guid?> configValue
        )
        {
            if (configValue.Value is not null)
                return configValue.Value.Value;

            var accountBinding = VerifiedNotNull(configValue.DataModelBinding);
            var dataElement = instance.GetRequiredDataElement(accountBinding.DataType);
            var data = await layoutState.GetModelData(accountBinding, dataElement, null);

            if (data is Guid guid)
                return guid;

            return Guid.TryParse($"{data}", out var recipient)
                ? recipient
                : throw new FiksArkivException(
                    $"Could not parse recipient account from data binding: {accountBinding}. Bound value resolved to `{data}` (of type `{data?.GetType()}`)"
                );
        }

        async Task<string?> GetOptionalValue(
            LayoutEvaluatorState layoutState,
            FiksArkivRecipientValue<string>? configValue
        )
        {
            if (configValue is null)
                return null;

            if (configValue.Value is not null)
                return configValue.Value;

            var recipientBinding = VerifiedNotNull(configValue.DataModelBinding);
            var dataElement = instance.GetRequiredDataElement(recipientBinding.DataType);
            var data = await layoutState.GetModelData(
                new ModelBinding { Field = recipientBinding.Field, DataType = recipientBinding.DataType },
                new DataElementIdentifier(dataElement.Id),
                null
            );

            return data as string
                ?? throw new FiksArkivException($"Could not parse recipient data binding: {recipientBinding}");
        }
    }

    private Korrespondansepart GetRecipientParty(Instance instance, RecipientWrapper recipient)
    {
        return new Korrespondansepart
        {
            KorrespondansepartID = recipient.Identifier,
            Korrespondanseparttype = new Korrespondanseparttype
            {
                KodeProperty = KorrespondanseparttypeKoder.Mottaker.Verdi,
                Beskrivelse = KorrespondanseparttypeKoder.Mottaker.Beskrivelse,
            },
            Organisasjonid = recipient.OrgNumber,
            KorrespondansepartNavn = recipient.Name,
            DeresReferanse = GetCorrelationId(instance),
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
                throw new FiksArkivException(
                    $"Could not determine sender details from authentication context: {_authenticationContext.Current}"
                );
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

    private sealed record RecipientWrapper(Guid AccountId, string? Identifier, string? OrgNumber, string? Name);
}
