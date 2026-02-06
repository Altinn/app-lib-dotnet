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

    public Task<List<NotificationReference>> NotifyInstanceOwner(Instance instance, EmailOverride emailNotification, SmsOverride smsNotification, CancellationToken ct)
    {
        throw new NotImplementedException();
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
        EmailNotification emailNotification,
        SmsNotification smsNotification,
        CancellationToken ct
    )
    {
        var notificationReferences = new List<NotificationReference>
        {
            await OrderEmail(emailNotification, ct),
            await OrderSms(smsNotification, ct)
        };

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
