using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly ISmsNotificationClient _smsNotificationClient;

    public NotificationService(
        IEmailNotificationClient emailNotificationClient,
        ISmsNotificationClient smsNotificationClient
    )
    {
        _emailNotificationClient = emailNotificationClient;
        _smsNotificationClient = smsNotificationClient;
    }

    public async Task<List<NotificationReference>> NotifyInstanceOwner(
        Instance instance,
        EmailOverride? emailOverride,
        SmsOverride? smsOverride,
        CancellationToken ct
    )
    {
        List<NotificationReference> notificationReferences = [];
        string instanceOwnerRecipient = instance.InstanceOwner?.PartyId ?? throw new InvalidOperationException("Instance owner must be set on instance to use email override");
        string sendersReference = $"instance-{instance.Id}";

        if (emailOverride is not null)
        {
            // TODO: parse body and subject text resources

            EmailNotification emailNotification = new()
            {
                Subject = emailOverride.SubjectTextResource,
                Body = emailOverride.BodyTextResource,
                Recipients = [new(instanceOwnerRecipient)],
                SendersReference = sendersReference
            };

            NotificationReference notificationReference = await OrderEmail(emailNotification, ct);
            notificationReferences.Add(notificationReference);
        }

        if (smsOverride is not null)
        {
            // TODO: parse body text resource

            SmsNotification smsNotification = new()
            {
                SenderNumber = smsOverride.SenderNumber,
                Body = smsOverride.BodyTextResource,
                Recipients = [new(instanceOwnerRecipient)],
                SendersReference = sendersReference
            };

            NotificationReference notificationReference = await OrderSms(smsNotification, ct);
            notificationReferences.Add(notificationReference);
        }

        return notificationReferences;
    }

    public async Task<List<NotificationReference>> ProcessNotificationOrders(
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
