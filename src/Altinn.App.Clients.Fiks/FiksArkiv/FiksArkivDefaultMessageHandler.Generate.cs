using System.Globalization;
using System.Text;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
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
    private async Task<IEnumerable<FiksIOMessagePayload>> GenerateMessagePayloads(Instance instance, Guid recipient)
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
                KodeProperty = JournalposttypeKoder.UtgaaendeDokument.Verdi,
                Beskrivelse = JournalposttypeKoder.UtgaaendeDokument.Beskrivelse,
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
        journalEntry.Dokumentbeskrivelse.Add(GetDocumentMetadata(archiveDocuments.FormDocument));

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

    private async Task<Guid> GetRecipient(Instance instance)
    {
        try
        {
            var configuredRecipient = VerifiedNotNull(_fiksArkivSettings.AutoSend?.Recipient);
            if (Guid.TryParse(configuredRecipient, out var recipient))
                return recipient;

            var recipientPathParts = configuredRecipient.Split('.');
            var dataType = recipientPathParts[0];
            var field = string.Join('.', recipientPathParts.Skip(1));
            var dataElement = instance.Data.First(x => x.DataType == dataType);

            var unitOfWork = await _instanceDataUnitOfWorkInitializer.Init(instance, null, null);
            var layoutState = await _layoutStateInitializer.Init(unitOfWork, null);
            var data = await layoutState.GetModelData(
                new ModelBinding { Field = field, DataType = dataType },
                new DataElementIdentifier(dataElement.Id),
                null
            );

            return Guid.TryParse(data as string, out recipient)
                ? recipient
                : throw new FiksArkivException($"Could not parse recipient from model query `{configuredRecipient}`");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv error: {Error}", e.Message);
            throw;
        }
    }

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
            // TODO: We need the correct recipient details here
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
}
