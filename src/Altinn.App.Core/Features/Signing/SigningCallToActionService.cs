using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Enums;
using Altinn.App.Core.Features.Signing.Helpers;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Signing;

internal sealed class SigningCallToActionService(
    ICorrespondenceClient correspondenceClient,
    IHostEnvironment hostEnvironment,
    IAppResources appResources,
    IAppMetadata appMetadata,
    IProfileClient profileClient,
    ILogger<SigningCallToActionService> logger,
    IOptions<GeneralSettings> settings
) : ISigningCallToActionService
{
    private readonly ICorrespondenceClient _correspondenceClient = correspondenceClient;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IAppResources _appResources = appResources;
    private readonly IAppMetadata _appMetadata = appMetadata;
    private readonly IProfileClient _profileClient = profileClient;
    private readonly ILogger<SigningCallToActionService> _logger = logger;
    private readonly UrlHelper _urlHelper = new(settings);

    public async Task<SendCorrespondenceResponse?> SendSignCallToAction(
        Notification? notification,
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        Party signingParty,
        Party serviceOwnerParty,
        List<AltinnEnvironmentConfig>? correspondenceResources
    )
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        HostingEnvironment env = AltinnEnvironments.GetHostingEnvironment(_hostEnvironment);
        var resource = AltinnTaskExtension.GetConfigForEnvironment(env, correspondenceResources)?.Value;
        if (string.IsNullOrEmpty(resource))
        {
            throw new ConfigurationException(
                $"No correspondence resource configured for environment {env}, skipping correspondence send"
            );
        }

        CorrespondanceRecipient recipient = new(signingParty);
        string instanceUrl = _urlHelper.GetInstanceUrl(appIdentifier, instanceIdentifier);
        UserProfile? recipientProfile = null;
        if (recipient.IsPerson)
        {
            try
            {
                recipientProfile = await _profileClient.GetUserProfile(recipient.SSN);
            }
            catch (Exception e)
            {
                _logger.LogWarning(
                    e,
                    "Unable to fetch profile for user with SSN, falling back to default values: {Exception}",
                    e.Message
                );
            }
        }
        string recipientLanguage = recipientProfile?.ProfileSettingPreference.Language ?? LanguageConst.Nb;
        ContentWrapper contentWrapper = await GetContent(
            notification,
            appIdentifier,
            applicationMetadata,
            serviceOwnerParty,
            instanceUrl,
            recipientLanguage
        );
        CorrespondenceContent correspondenceContent = contentWrapper.CorrespondenceContent;
        string? emailBody = contentWrapper.EmailBody;
        string? emailSubject = contentWrapper.EmailSubject;
        string? smsBody = contentWrapper.SmsBody;

        return await _correspondenceClient.Send(
            new SendCorrespondencePayload(
                CorrespondenceRequestBuilder
                    .Create()
                    .WithResourceId(resource)
                    .WithSender(serviceOwnerParty.OrgNumber) // Will fail if using ttd, as it has no org number
                    .WithSendersReference(instanceIdentifier.ToString())
                    .WithRecipient(recipient.IsPerson ? recipient.SSN : recipient.OrganisationNumber)
                    .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                    .WithContent(correspondenceContent)
                    .WithNotificationIfConfigured(
                        SigningCorrespondanceHelper.GetNotificationChoice(notification) switch
                        {
                            NotificationChoice.Email => new CorrespondenceNotification
                            {
                                NotificationTemplate = emailBody is not null
                                    ? CorrespondenceNotificationTemplate.CustomMessage
                                    : CorrespondenceNotificationTemplate.GenericAltinnMessage,
                                NotificationChannel = CorrespondenceNotificationChannel.Email,
                                EmailSubject = emailSubject ?? correspondenceContent.Title,
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
                                NotificationChannel = CorrespondenceNotificationChannel.EmailPreferred, // TODO: document
                                EmailSubject = emailSubject,
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
    }

    internal async Task<ContentWrapper> GetContent(
        Notification? notification,
        AppIdentifier appIdentifier,
        ApplicationMetadata appMetadata,
        Party senderParty,
        string instanceUrl,
        string language
    )
    {
        TextResource? textResource = null;
        string? correspondenceTitle = null;
        string? correspondenceSummary = null;
        string? correspondenceBody = null;
        string? smsBody = null;
        string? emailBody = null;
        string? emailSubject = null;
        string? appName = null;

        string appOwner = senderParty.Name ?? appMetadata.Org;
        string defaultAppName =
            appMetadata.Title?.GetValueOrDefault(language)
            ?? appMetadata.Title?.FirstOrDefault().Value
            ?? appMetadata.Id;

        try
        {
            textResource ??=
                await _appResources.GetTexts(appIdentifier.Org, appIdentifier.App, language)
                ?? throw new InvalidOperationException($"No text resource found for language ({language})");

            correspondenceTitle = textResource.GetText("signing.cta_title"); // TODO: Document these text keys
            correspondenceSummary = textResource.GetText("signing.cta_summary"); // TODO: Document these text keys
            correspondenceBody = textResource.GetText("signing.cta_body"); // TODO: Document these text keys
            correspondenceBody = correspondenceBody?.Replace(
                "$InstanceUrl",
                instanceUrl,
                StringComparison.InvariantCultureIgnoreCase
            );
            appName = textResource.GetFirstMatchingText("appName", "ServiceName");

            smsBody = textResource.GetText(notification?.Sms?.TextResourceKey);
            emailBody = textResource.GetText(notification?.Email?.BodyTextResourceKey);
            emailSubject = textResource.GetText(notification?.Email?.SubjectTextResourceKey);
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

        var defaults = GetDefaultTexts(instanceUrl, language, appName, appOwner);
        ContentWrapper contentWrapper = new()
        {
            CorrespondenceContent = new CorrespondenceContent()
            {
                Language = LanguageCode<Iso6391>.Parse(textResource?.Language ?? language),
                Title = correspondenceTitle ?? defaults.Title,
                Summary = correspondenceSummary ?? defaults.Summary,
                Body = correspondenceBody ?? defaults.Body,
            },
            SmsBody = smsBody ?? defaults.SmsBody,
            EmailBody = emailBody ?? defaults.EmailBody,
            EmailSubject = emailSubject ?? defaults.Title,
        };
        return contentWrapper;
    }

    /// <summary>
    /// Gets the default texts for the given language.
    /// </summary>
    /// <param name="instanceUrl">The url for the instance</param>
    /// <param name="language">The language to get the texts for</param>
    /// <param name="appName">The name of the app</param>
    /// <param name="appOwner">The owner of the app</param>
    internal static DefaultTexts GetDefaultTexts(string instanceUrl, string language, string appName, string appOwner)
    {
        return language switch
        {
            LanguageConst.En => new DefaultTexts
            {
                Title = $"{appName}: Task for signing",
                Summary = $"Your signature is requested for {appName}.",
                Body =
                    $"You have a task waiting for your signature. <a href=\"{instanceUrl}\">Click here to open the application</a>.<br /><br />If you have any questions, you can contact {appOwner}.",
                SmsBody = $"Your signature is requested for {appName}. Open your Altinn inbox to proceed.",
                EmailSubject = $"{appName}: Task for signing",
                EmailBody =
                    $"Your signature is requested for {appName}. Open your Altinn inbox to proceed.<br /><br />If you have any questions, you can contact {appOwner}.",
            },
            LanguageConst.Nn => new DefaultTexts
            {
                Title = $"{appName}: Oppgåve til signering",
                Summary = $"Signaturen din vert venta for {appName}.",
                Body =
                    $"Du har ei oppgåve som ventar på signaturen din. <a href=\"{instanceUrl}\">Klikk her for å opne applikasjonen</a>.<br /><br />Om du lurer på noko, kan du kontakte {appOwner}.",
                SmsBody = $"Signaturen din vert venta for {appName}. Opne Altinn-innboksen din for å gå vidare.",
                EmailSubject = $"{appName}: Oppgåve til signering",
                EmailBody =
                    $"Signaturen din vert venta for {appName}. Opne Altinn-innboksen din for å gå vidare.<br /><br />Om du lurer på noko, kan du kontakte {appOwner}.",
            },
            LanguageConst.Nb or _ => new DefaultTexts
            {
                Title = $"{appName}: Oppgave til signering",
                Summary = $"Din signatur ventes for {appName}.",
                Body =
                    $"Du har en oppgave som venter på din signatur. <a href=\"{instanceUrl}\">Klikk her for å åpne applikasjonen</a>.<br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
                SmsBody = $"Din signatur ventes for {appName}. Åpne Altinn-innboksen din for å fortsette.",
                EmailSubject = $"{appName}: Oppgave til signering",
                EmailBody =
                    $"Din signatur ventes for {appName}. Åpne Altinn-innboksen din for å fortsette.<br /><br />Hvis du lurer på noe, kan du kontakte {appOwner}.",
            },
        };
    }
}
