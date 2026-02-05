using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;

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

    public Task<NotificationReference> ProcessNotificationOrder(
        EmailNotification emailNotification,
        CancellationToken ct
    )
    {
        return OrderEmail(emailNotification, ct);
    }

    public Task<NotificationReference> ProcessNotificationOrder(SmsNotification smsNotification, CancellationToken ct)
    {
        return OrderSms(smsNotification, ct);
    }

    public async Task<List<NotificationReference>> ProcessNotificationOrders(
        List<EmailNotification> emailNotifications,
        List<SmsNotification> smsNotifications,
        CancellationToken ct
    )
    {
        var notificationReferences = new List<NotificationReference>();

        Parallel.ForEach(
            emailNotifications,
            async emailNotification => notificationReferences.Add(await OrderEmail(emailNotification, ct))
        );
        Parallel.ForEach(
            smsNotifications,
            async smsNotification => notificationReferences.Add(await OrderSms(smsNotification, ct))
        );

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
