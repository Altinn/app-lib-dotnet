using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly ISmsNotificationClient _smsNotificationClient;
    private readonly ITranslationService _translationService;

    public NotificationService(
        IEmailNotificationClient emailNotificationClient,
        ISmsNotificationClient smsNotificationClient,
        ITranslationService translationService
    )
    {
        _emailNotificationClient = emailNotificationClient;
        _smsNotificationClient = smsNotificationClient;
        _translationService = translationService;
    }

    public async Task<List<NotificationReference>> NotifyInstanceOwner(
        string language,
        Instance instance,
        EmailOverride? emailOverride,
        SmsOverride? smsOverride,
        CancellationToken ct
    )
    {
        List<NotificationReference> notificationReferences = [];
        string instanceOwnerRecipient =
            instance.InstanceOwner?.PartyId
            ?? throw new InvalidOperationException("Instance owner must be set on instance to use email override");
        string sendersReference = $"instance-{instance.Id}";

        if (emailOverride is not null)
        {
            string defaultSubjectTextResourceId = BackendTextResource.EmailDefaultSubject;
            string defaultBodyTextResourceId = BackendTextResource.EmailDefaultBody;
            string subject = await GetTextResource(
                language,
                defaultSubjectTextResourceId,
                emailOverride.SubjectTextResource
            );
            string body = await GetTextResource(language, defaultBodyTextResourceId, emailOverride.BodyTextResource);

            EmailNotification emailNotification = new()
            {
                Subject = subject,
                Body = body,
                Recipients = [new(instanceOwnerRecipient)],
                SendersReference = sendersReference,
            };

            NotificationReference notificationReference = await OrderEmail(emailNotification, ct);
            notificationReferences.Add(notificationReference);
        }

        if (smsOverride is not null)
        {
            string defaultBodyTextResourceId = BackendTextResource.SmsDefaultBody;
            string body = await GetTextResource(language, defaultBodyTextResourceId, smsOverride.BodyTextResource);

            SmsNotification smsNotification = new()
            {
                SenderNumber = smsOverride.SenderNumber,
                Body = body,
                Recipients = [new(instanceOwnerRecipient)],
                SendersReference = sendersReference,
            };

            NotificationReference notificationReference = await OrderSms(smsNotification, ct);
            notificationReferences.Add(notificationReference);
        }

        return notificationReferences;
    }

    public async Task<List<NotificationReference>> ProcessNotificationOrders(
        string language,
        List<EmailNotification> emailNotifications,
        List<SmsNotification> smsNotifications,
        CancellationToken ct
    )
    {
        var notificationReferences = new List<NotificationReference>();
        foreach (EmailNotification emailNotification in emailNotifications)
        {
            notificationReferences.Add(await OrderEmail(emailNotification, ct));
        }

        foreach (SmsNotification smsNotification in smsNotifications)
        {
            notificationReferences.Add(await OrderSms(smsNotification, ct));
        }

        return notificationReferences;
    }

    private async Task<string> GetTextResource(
        string language,
        string defaultTextResourceId,
        string? textResourceId = null
    )
    {
        string? translatedText =
            await _translationService.TranslateTextKey(language, textResourceId ?? defaultTextResourceId)
            ?? throw new InvalidOperationException(
                $"Default text resource '{defaultTextResourceId}' could not be found for language '{language}'"
            );
        return translatedText;
    }

    private async Task<NotificationReference> OrderEmail(EmailNotification emailNotification, CancellationToken ct)
    {
        EmailOrderResponse response = await _emailNotificationClient.Order(emailNotification, ct);
        return new NotificationReference(emailNotification.SendersReference, response.OrderId);
    }

    private async Task<NotificationReference> OrderSms(SmsNotification smsNotification, CancellationToken ct)
    {
        SmsOrderResponse response = await _smsNotificationClient.Order(smsNotification, ct);
        return new NotificationReference(smsNotification.SendersReference, response.OrderId);
    }
}
