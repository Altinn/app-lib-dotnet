using System.Globalization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Enums;
using Altinn.App.Core.Features.Signing.Helpers;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningCorrespondenceService(
    ICorrespondenceClient correspondenceClient,
    IDataClient dataClient,
    IHostEnvironment hostEnvironment,
    IAppResources appResources,
    IAppMetadata appMetadata,
    ILogger<SigningCorrespondenceService> logger,
    IOptions<GeneralSettings> settings
) : ISigningCorrespondenceService
{
    private readonly ICorrespondenceClient _correspondenceClient = correspondenceClient;
    private readonly IDataClient _dataClient = dataClient;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IAppResources _appResources = appResources;
    private readonly IAppMetadata _appMetadata = appMetadata;
    private readonly ILogger<SigningCorrespondenceService> _logger = logger;
    private readonly UrlHelper _urlHelper = new(settings);

    public async Task<SendCorrespondenceResponse?> SendSignConfirmationCorrespondence(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        UserActionContext context,
        List<AltinnEnvironmentConfig>? correspondenceResources
    )
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        var (resource, senderOrgNumber, senderDetails, recipient) = await GetCorrespondenceHeaders(
            signee.PersonNumber,
            appMetadata,
            correspondenceResources,
            context.AltinnCdnClient
        );

        CorrespondenceContent content = await GetContent(context, appMetadata, senderDetails);
        IEnumerable<CorrespondenceAttachment> attachments = await GetCorrespondenceAttachments(
            instanceIdentifier,
            dataElementSignatures,
            appMetadata,
            context,
            _dataClient
        );

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(
                CorrespondenceRequestBuilder
                    .Create()
                    .WithResourceId(resource)
                    .WithSender(senderOrgNumber)
                    .WithSendersReference(instanceIdentifier.ToString())
                    .WithRecipient(recipient)
                    .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                    .WithContent(content)
                    .WithAttachments(attachments)
                    .Build(),
                CorrespondenceAuthorisation.Maskinporten
            )
        );

        async Task<(
            string resource,
            string senderOrgNumber,
            AltinnCdnOrgDetails senderDetails,
            string recipient
        )> GetCorrespondenceHeaders(
            string? recipientNin,
            ApplicationMetadata appMetadata,
            List<AltinnEnvironmentConfig>? correspondenceResources,
            IAltinnCdnClient? altinnCdnClient = null
        )
        {
            HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
            var resource = AltinnTaskExtension.GetConfigForEnvironment(env, correspondenceResources)?.Value;
            if (string.IsNullOrEmpty(resource))
            {
                throw new ConfigurationException(
                    $"No correspondence resource configured for environment {env}, skipping correspondence send"
                );
            }

            string? recipient = recipientNin;
            if (string.IsNullOrEmpty(recipient))
            {
                throw new InvalidOperationException(
                    "Signee's national identity number is missing, unable to send correspondence"
                );
            }

            bool disposeClient = altinnCdnClient is null;
            altinnCdnClient ??= new AltinnCdnClient();
            try
            {
                AltinnCdnOrgs altinnCdnOrgs = await altinnCdnClient.GetOrgs();
                AltinnCdnOrgDetails? senderDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org);
                string? senderOrgNumber = senderDetails?.Orgnr;

                if (senderDetails is null || string.IsNullOrEmpty(senderOrgNumber))
                {
                    throw new InvalidOperationException(
                        $"Error looking up sender's organisation number from Altinn CDN, using key `{appMetadata.Org}`"
                    );
                }

                return (resource, senderOrgNumber, senderDetails, recipient);
            }
            finally
            {
                if (disposeClient)
                {
                    altinnCdnClient.Dispose();
                }
            }
        }

        async Task<CorrespondenceContent> GetContent(
            UserActionContext context,
            ApplicationMetadata appMetadata,
            AltinnCdnOrgDetails senderDetails
        )
        {
            TextResource? textResource = null;
            string? title = null;
            string? summary = null;
            string? body = null;
            string? appName = null;

            string appOwner =
                senderDetails.Name?.Nb ?? senderDetails.Name?.Nn ?? senderDetails.Name?.En ?? appMetadata.Org;
            string defaultLanguage = LanguageConst.Nb;
            string defaultAppName =
                appMetadata.Title?.GetValueOrDefault(defaultLanguage)
                ?? appMetadata.Title?.FirstOrDefault().Value
                ?? appMetadata.Id;

            try
            {
                AppIdentifier appIdentifier = new(context.Instance);

                textResource ??=
                    await _appResources.GetTexts(
                        appIdentifier.Org,
                        appIdentifier.App,
                        context.Language ?? defaultLanguage
                    )
                    ?? throw new InvalidOperationException(
                        $"No text resource found for specified language ({context.Language}) nor the default language ({defaultLanguage})"
                    );

                title = textResource.GetText("signing.receipt_title");
                summary = textResource.GetText("signing.receipt_summary");
                body = textResource.GetText("signing.receipt_body");
                appName = textResource.GetFirstMatchingText("appName", "ServiceName");
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "Unable to fetch custom message correspondence message content, falling back to default values: {Exception}",
                    e.Message
                );
            }

            if (string.IsNullOrWhiteSpace(appName))
            {
                appName = defaultAppName;
            }

            var defaults = new
            {
                Title = $"{appName}: Signeringen er bekreftet",
                Summary = $"Du har signert for {appName}.",
                Body = $"Dokumentene du har signert er vedlagt. Disse kan lastes ned om ønskelig. <br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
            };

            CorrespondenceContent content = new()
            {
                Language = LanguageCode<Iso6391>.Parse(textResource?.Language ?? defaultLanguage),
                Title = title ?? defaults.Title,
                Summary = summary ?? defaults.Summary,
                Body = body ?? defaults.Body,
            };
            return content;
        }
    }

    public async Task<SendCorrespondenceResponse?> SendSignCallToActionCorrespondence(
        Notification? notification,
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        Party signingParty,
        Party serviceOwnerParty,
        List<AltinnEnvironmentConfig>? correspondenceResources
    )
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
        var resource = AltinnTaskExtension.GetConfigForEnvironment(env, correspondenceResources)?.Value;
        if (string.IsNullOrEmpty(resource))
        {
            throw new ConfigurationException(
                $"No correspondence resource configured for environment {env}, skipping correspondence send"
            );
        }

        string? recipient = signingParty.SSN;
        if (string.IsNullOrEmpty(recipient))
        {
            throw new InvalidOperationException(
                "Signee's national identity number is missing, unable to send correspondence"
            );
        }

        string instanceUrl = _urlHelper.GetInstanceUrl(appIdentifier, instanceIdentifier);
        CorrespondenceContent content = await GetContent(appIdentifier, appMetadata, serviceOwnerParty, instanceUrl);
        string? emailBody = notification?.Email?.Body;
        string? emailSubject = notification?.Email?.Subject;
        string? smsBody = notification?.Sms?.Body;


        // TODO: Language support
        // TODO: Tests
        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(
                CorrespondenceRequestBuilder
                    .Create()
                    .WithResourceId(resource)
                    .WithSender(serviceOwnerParty.OrgNumber)
                    .WithSendersReference(instanceIdentifier.ToString())
                    .WithRecipient(recipient)
                    .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                    .WithContent(content)
                    .WithNotificationIfConfigured(
                        SigningCorrespondanceHelper.GetNotificationChoice(notification) switch
                        {
                            NotificationChoice.Email => new CorrespondenceNotification
                            {
                                NotificationTemplate = emailBody is not null
                                    ? CorrespondenceNotificationTemplate.CustomMessage
                                    : CorrespondenceNotificationTemplate.GenericAltinnMessage,
                                NotificationChannel = CorrespondenceNotificationChannel.Email,
                                EmailSubject = emailSubject ?? content.Title,
                                EmailBody = emailBody,
                                SendersReference = instanceIdentifier.ToString(),
                                SendReminder = true,
                            },
                            NotificationChoice.Sms => new CorrespondenceNotification
                            {
                                NotificationTemplate = smsBody is not null
                                    ? CorrespondenceNotificationTemplate.CustomMessage
                                    : CorrespondenceNotificationTemplate.GenericAltinnMessage,
                                NotificationChannel = CorrespondenceNotificationChannel.Sms,
                                SmsBody = smsBody,
                                SendersReference = instanceIdentifier.ToString(),
                                SendReminder = true,
                            },
                            NotificationChoice.SmsAndEmail => new CorrespondenceNotification
                            {
                                NotificationTemplate = emailBody is not null
                                    ? CorrespondenceNotificationTemplate.CustomMessage
                                    : CorrespondenceNotificationTemplate.GenericAltinnMessage,
                                NotificationChannel = CorrespondenceNotificationChannel.EmailPreferred,
                                EmailSubject = emailSubject ?? content.Title,
                                EmailBody = emailBody,
                                SmsBody = smsBody,
                                SendersReference = instanceIdentifier.ToString(),
                                SendReminder = true,
                            },
                            NotificationChoice.None or _ => null,
                        }
                    )
                    .Build(),
                CorrespondenceAuthorisation.Maskinporten
            )
        );

        async Task<CorrespondenceContent> GetContent(
            AppIdentifier appIdentifier,
            ApplicationMetadata appMetadata,
            Party senderParty,
            string instanceUrl
        )
        {
            TextResource? textResource = null;
            string? title = null;
            string? summary = null;
            string? body = null;
            string? appName = null;

            string appOwner = senderParty.Name ?? appMetadata.Org;
            string defaultLanguage = LanguageConst.Nb;
            string defaultAppName =
                appMetadata.Title?.GetValueOrDefault(defaultLanguage)
                ?? appMetadata.Title?.FirstOrDefault().Value
                ?? appMetadata.Id;

            try
            {
                textResource ??=
                    await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, defaultLanguage)
                    ?? throw new InvalidOperationException(
                        $"No text resource found for the default language ({defaultLanguage})"
                    );

                title = textResource.GetText("signing.cta_title");
                summary = textResource.GetText("signing.cta_summary");
                body = textResource.GetText("signing.cta_body");
                appName = textResource.GetFirstMatchingText("appName", "ServiceName");
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "Unable to fetch custom message correspondence message content, falling back to default values: {Exception}",
                    e.Message
                );
            }

            if (string.IsNullOrWhiteSpace(appName))
            {
                appName = defaultAppName;
            }

            var defaults = new
            {
                Title = $"{appName}: Oppgave til signering",
                Summary = $"Din signatur ventes for {appName}.",
                Body = $"Du har en oppgave som venter på din signatur. <a href=\"{instanceUrl}\">Klikk her for å åpne applikasjonen</a>.<br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
            };

            CorrespondenceContent content = new()
            {
                Language = LanguageCode<Iso6391>.Parse(textResource?.Language ?? defaultLanguage),
                Title = title ?? defaults.Title,
                Summary = summary ?? defaults.Summary,
                Body = body ?? defaults.Body,
            };
            return content;
        }
    }

    internal static async Task<IEnumerable<CorrespondenceAttachment>> GetCorrespondenceAttachments(
        InstanceIdentifier instanceIdentifier,
        IEnumerable<DataElementSignature> dataElementSignatures,
        ApplicationMetadata appMetadata,
        UserActionContext context,
        IDataClient dataClient
    )
    {
        List<CorrespondenceAttachment> attachments = [];
        IEnumerable<DataElement> signedElements = context.Instance.Data.Where(IsSignedDataElement);

        foreach (DataElement element in signedElements)
        {
            string filename = GetDataElementFilename(element, appMetadata);

            attachments.Add(
                CorrespondenceAttachmentBuilder
                    .Create()
                    .WithFilename(filename)
                    .WithName(filename)
                    .WithSendersReference(element.Id)
                    .WithDataType(element.ContentType ?? "application/octet-stream")
                    .WithData(
                        await dataClient.GetDataBytes(
                            appMetadata.AppIdentifier.Org,
                            appMetadata.AppIdentifier.App,
                            instanceIdentifier.InstanceOwnerPartyId,
                            instanceIdentifier.InstanceGuid,
                            Guid.Parse(element.Id)
                        )
                    )
                    .Build()
            );
        }

        return attachments;

        bool IsSignedDataElement(DataElement dataElement) =>
            dataElementSignatures.Any(x => x.DataElementId == dataElement.Id);
    }

    /// <summary>
    /// Note: This method contains only an extremely small list of known mime types.
    /// The aim here is not to be exhaustive, just to cover some common cases.
    /// </summary>
    internal static string GetDataElementFilename(DataElement dataElement, ApplicationMetadata appMetadata)
    {
        if (!string.IsNullOrWhiteSpace(dataElement.Filename))
        {
            return dataElement.Filename;
        }

        DataType? dataType = appMetadata.DataTypes.Find(x => x.Id == dataElement.DataType);

        var mimeType = dataElement.ContentType?.ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
        var formDataExtensions = new[] { ".xml", ".json" };
        var mapping = new Dictionary<string, string>
        {
            ["application/xml"] = ".xml",
            ["text/xml"] = ".xml",
            ["application/pdf"] = ".pdf",
            ["application/json"] = ".json",
        };

        string? extension = mapping.GetValueOrDefault(mimeType);
        string filename = dataElement.DataType;
        if (dataType?.AppLogic is not null && formDataExtensions.Contains(extension))
        {
            filename = $"skjemadata_{filename}";
        }

        return $"{filename}{extension}";
    }
}
