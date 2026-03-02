using Altinn.App.Core.Features.Notifications.Texts;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models.Notifications.Future;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly IRegisterClient _registerClient;
    private readonly INotificationOrderClient _notificationOrderClient;
    private readonly IProfileClient _profileClient;
    private readonly IAltinnCdnClient _cdnClient;
    private readonly IAltinnPartyClient _altinnPartyClient;

    public NotificationService(
    INotificationOrderClient notificationOrderClient,
    IProfileClient profileClient,
    IAltinnCdnClient cdnClient,
    IAltinnPartyClient altinnPartyClient,
    IServiceProvider serviceProvider
    )
    {
        _registerClient = serviceProvider.GetRequiredService<IRegisterClient>();
        _notificationOrderClient = notificationOrderClient;
        _profileClient = profileClient;
        _cdnClient = cdnClient;
        _altinnPartyClient = altinnPartyClient;
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

        NotificationOrderRequest orderRequest = CreateNotificationOrderRequest(
            language,
            instance,
            party.Name,
            serviceOwnerName,
            instansiationNotification
        );

        await _notificationOrderClient.Order(orderRequest, ct);
    }

    internal static NotificationOrderRequest CreateNotificationOrderRequest(
        string language,
        Instance instance,
        string? instanceOwnerName,
        AltinnCdnOrgName? serviceOwnerName,
        InstansiationNotification instansiationNotification
    )
    {
        InstanceOwner instanceOwner = instance.InstanceOwner;
        DateOnly? dueDateString = instance.DueBefore.HasValue ? DateOnly.FromDateTime(instance.DueBefore.Value) : null;

        CustomEmail? customEmail = instansiationNotification.CustomEmail;
        EmailSendingOptions emailSettings = new()
        {
            SendingTimePolicy = SendingTimePolicy.Anytime,
            Subject = customEmail is not null
                ? NotificationTexts.ReplaceTokens(
                    text: customEmail.Subject.GetTextForLanguage(language),
                    appId: instance.AppId,
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

        if (instanceOwner.OrganisationNumber is not null)
        {
            return new NotificationOrderRequest
            {
                SendersReference = instance.Id + instanceOwner.OrganisationNumber,
                IdempotencyId = instance.Id + instanceOwner.OrganisationNumber,
                Recipient = new NotificationRecipient
                {
                    RecipientOrganization = new RecipientOrganization
                    {
                        OrgNumber = instanceOwner.OrganisationNumber,
                        ChannelSchema = requestedChannel,
                        EmailSettings = emailSettings,
                        SmsSettings = smsSettings,
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
                Recipient = new NotificationRecipient
                {
                    RecipientPerson = new RecipientPerson
                    {
                        NationalIdentityNumber = instanceOwner.PersonNumber,
                        ChannelSchema = requestedChannel,
                        EmailSettings = emailSettings,
                        SmsSettings = smsSettings,
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
                Recipient = new NotificationRecipient
                {
                    RecipientSelfIdentifiedUser = new RecipientSelfIdentifiedUser
                    {
                        ExternalIdentity = instanceOwner.ExternalIdentifier,
                        ChannelSchema = NotificationChannel.Email, // Only email is supported for self identified users
                        EmailSettings = emailSettings,
                    },
                },
            };
        }

        throw new InvalidOperationException(
            "InstanceOwner must have at least one of OrganisationNumber, PersonNumber, or ExternalIdentifier set."
        );
    }

    internal async Task<string> DetermineLanguage(InstanceOwner instanceOwner, string? requestedOrgLanguage, CancellationToken ct)
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
