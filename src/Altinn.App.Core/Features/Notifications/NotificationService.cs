using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Notifications.Texts;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Notifications.Future;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly INotificationOrderClient _notificationOrderClient;
    private readonly IProfileClient _profileClient;
    private readonly IAltinnCdnClient _cdnClient;
    private readonly IAppMetadata _appMetadata;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly GeneralSettings _generalSettings;

    public NotificationService(
        INotificationOrderClient notificationOrderClient,
        IProfileClient profileClient,
        IAltinnCdnClient cdnClient,
        IAppMetadata appMetadata,
        IAltinnPartyClient altinnPartyClient,
        IOptions<GeneralSettings> generalSettings
    )
    {
        _notificationOrderClient = notificationOrderClient;
        _profileClient = profileClient;
        _cdnClient = cdnClient;
        _appMetadata = appMetadata;
        _altinnPartyClient = altinnPartyClient;
        _generalSettings = generalSettings.Value;
    }

    public async Task NotifyInstanceOwnerOnInstansiation(
        Instance instance,
        Party party,
        InstansiationNotification instansiationNotification,
        CancellationToken ct
    )
    {
        InstanceOwner instanceOwner = instance.InstanceOwner;
        string? language = await DetermineLanguage(instanceOwner, instansiationNotification.Language, ct);
        AltinnCdnOrgName? serviceOwnerName = await _cdnClient.GetOrgNameByAppId(instance.AppId, ct);
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
        string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance.AppId));

        NotificationOrderRequest orderRequest = CreateNotificationOrderRequest(
            language,
            instance,
            appMetadata,
            party.Name,
            serviceOwnerName,
            instansiationNotification,
            baseUrl
        );

        await _notificationOrderClient.Order(orderRequest, ct);
    }

    internal static NotificationOrderRequest CreateNotificationOrderRequest(
        string language,
        Instance instance,
        ApplicationMetadata? applicationMetadata,
        string? instanceOwnerName,
        AltinnCdnOrgName? serviceOwnerName,
        InstansiationNotification instansiationNotification,
        string? callBackBaseUrl
    )
    {
        InstanceOwner instanceOwner = instance.InstanceOwner;
        DateOnly? dueDateString = instance.DueBefore.HasValue ? DateOnly.FromDateTime(instance.DueBefore.Value) : null;
        string? appTitle = GetTitleFromMetadata(language, applicationMetadata);

        CustomEmail? customEmail = instansiationNotification.CustomEmail;
        EmailSendingOptions emailSettings = new()
        {
            SendingTimePolicy = SendingTimePolicy.Anytime,
            Subject = customEmail is not null
                ? NotificationTexts.ReplaceTokens(
                    text: customEmail.Subject.GetTextForLanguage(language),
                    appId: instance.AppId,
                    title: appTitle,
                    instanceOwnerName: instanceOwnerName,
                    serviceOwnerName: serviceOwnerName?.GetByLanguage(language),
                    orgNumber: instanceOwner.OrganisationNumber,
                    socialSecurityNumber: instanceOwner.PersonNumber,
                    dueDate: dueDateString
                )
                : NotificationTexts.GetDefaultSubject(language),
            Body = customEmail is not null
                ? NotificationTexts.ReplaceTokens(
                    text: customEmail.Body.GetTextForLanguage(language),
                    appId: instance.AppId,
                    title: appTitle,
                    instanceOwnerName: instanceOwnerName,
                    serviceOwnerName: serviceOwnerName?.GetByLanguage(language),
                    orgNumber: instanceOwner.OrganisationNumber,
                    socialSecurityNumber: instanceOwner.PersonNumber,
                    dueDate: dueDateString
                )
                : NotificationTexts.GetDefaultBody(
                    language: language,
                    appid: instance.AppId,
                    instanceOwnerName: instanceOwnerName,
                    serviceOwnerName: serviceOwnerName?.GetByLanguage(language),
                    orgNumber: instanceOwner.OrganisationNumber,
                    socialSecurityNumber: instanceOwner.PersonNumber,
                    dueDate: dueDateString
                ),
        };

        CustomSms? customSms = instansiationNotification.CustomSms;
        SmsSendingOptions smsSettings = new()
        {
            SendingTimePolicy = SendingTimePolicy.Anytime,
            Sender = customSms?.SenderName ?? "Altinn",
            Body = customSms is not null
                ? NotificationTexts.ReplaceTokens(
                    text: customSms.Text.GetTextForLanguage(language),
                    appId: instance.AppId,
                    title: appTitle,
                    instanceOwnerName: instanceOwnerName,
                    serviceOwnerName: serviceOwnerName?.GetByLanguage(language),
                    orgNumber: instanceOwner.OrganisationNumber,
                    socialSecurityNumber: instanceOwner.PersonNumber,
                    dueDate: dueDateString
                )
                : NotificationTexts.GetDefaultBody(
                    language: language,
                    appid: instance.AppId,
                    instanceOwnerName: instanceOwnerName,
                    serviceOwnerName: serviceOwnerName?.GetByLanguage(language),
                    orgNumber: instanceOwner.OrganisationNumber,
                    socialSecurityNumber: instanceOwner.PersonNumber,
                    dueDate: dueDateString
                ),
        };
        NotificationChannel requestedChannel = instansiationNotification.NotificationChannel;

        AppResourceId resourceId = AppResourceId.FromAppIdentifier(new(instance.AppId));
        DateTime requestedSendTimeOrDefault = instansiationNotification.RequestedSendTime ?? DateTime.Now.AddMinutes(5);

        Uri? conditionEndpoint = null;
        if (instansiationNotification.RequestedSendTime is not null)
        {
            conditionEndpoint = new Uri(callBackBaseUrl?.TrimEnd('/') + "/notifications/" + instance.Id);
        }

        if (instanceOwner.OrganisationNumber is not null)
        {
            return new NotificationOrderRequest
            {
                SendersReference = instance.Id + instanceOwner.OrganisationNumber,
                IdempotencyId = instance.Id + instanceOwner.OrganisationNumber,
                RequestedSendTime = requestedSendTimeOrDefault,
                ConditionEndpoint = conditionEndpoint,
                Recipient = new NotificationRecipient
                {
                    RecipientOrganization = new RecipientOrganization
                    {
                        OrgNumber = instanceOwner.OrganisationNumber,
                        ChannelSchema = requestedChannel,
                        EmailSettings = emailSettings,
                        SmsSettings = smsSettings,
                        ResourceId = resourceId.AsUrn,
                    },
                },
            };
        }

        if (instanceOwner.PersonNumber is not null)
        {
            return new NotificationOrderRequest
            {
                SendersReference = instance.Id + instanceOwner.PersonNumber,
                IdempotencyId = instance.Id + instanceOwner.PersonNumber,
                RequestedSendTime = requestedSendTimeOrDefault,
                ConditionEndpoint = conditionEndpoint,
                Recipient = new NotificationRecipient
                {
                    RecipientPerson = new RecipientPerson
                    {
                        NationalIdentityNumber = instanceOwner.PersonNumber,
                        ChannelSchema = requestedChannel,
                        EmailSettings = emailSettings,
                        SmsSettings = smsSettings,
                        ResourceId = resourceId.AsUrn,
                    },
                },
            };
        }

        if (instanceOwner.ExternalIdentifier is not null)
        {
            return new NotificationOrderRequest
            {
                SendersReference = instance.Id + instanceOwner.ExternalIdentifier,
                IdempotencyId = instance.Id + instanceOwner.ExternalIdentifier,
                RequestedSendTime = requestedSendTimeOrDefault,
                ConditionEndpoint = conditionEndpoint,
                Recipient = new NotificationRecipient
                {
                    RecipientSelfIdentifiedUser = new RecipientSelfIdentifiedUser
                    {
                        ExternalIdentity = instanceOwner.ExternalIdentifier,
                        ChannelSchema = NotificationChannel.Email, // Only email is supported for self identified users
                        EmailSettings = emailSettings,
                        ResourceId = resourceId.AsUrn,
                    },
                },
            };
        }

        throw new InvalidOperationException(
            "InstanceOwner must have at least one of OrganisationNumber, PersonNumber, or ExternalIdentifier set."
        );
    }

    internal static string? GetTitleFromMetadata(string language, ApplicationMetadata? applicationMetadata)
    {
        if (
            applicationMetadata?.UnmappedProperties?.TryGetValue("title", out object? titleObj) == true
            && titleObj is System.Text.Json.JsonElement titleElement
            && titleElement.TryGetProperty(language, out var titleForLanguage)
        )
        {
            return titleForLanguage.GetString();
        }
        return null;
    }

    internal async Task<string> DetermineLanguage(
        InstanceOwner instanceOwner,
        string? requestedOrgLanguage,
        CancellationToken ct = default
    )
    {
        if (instanceOwner.PersonNumber is not null)
        {
            UserProfile? personProfile = await _profileClient.GetUserProfile(instanceOwner.PersonNumber);
            return personProfile?.ProfileSettingPreference.Language ?? LanguageConst.Nb;
        }

        if (instanceOwner.ExternalIdentifier is not null)
        {
            Guid? partyGuid = await _altinnPartyClient.GetPartyUuidByUrn(instanceOwner.ExternalIdentifier);
            if (partyGuid is null)
            {
                return LanguageConst.En;
            }

            // HACK: userUuid == partyGuid
            UserProfile? userProfile = await _profileClient.GetUserProfile(partyGuid.Value);

            return userProfile?.ProfileSettingPreference.Language ?? LanguageConst.En;
        }

        if (instanceOwner.OrganisationNumber is not null)
        {
            return requestedOrgLanguage ?? LanguageConst.Nb;
        }

        throw new InvalidOperationException(
            "InstanceOwner must have at least one of OrganisationNumber, PersonNumber, or ExternalIdentifier set."
        );
    }
}
