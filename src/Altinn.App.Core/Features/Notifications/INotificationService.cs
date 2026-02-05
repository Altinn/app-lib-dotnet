using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;

namespace Altinn.App.Core.Features.Notifications;

internal interface INotificationService
{
    Task<List<NotificationReference>> ProcessNotificationOrders(
        List<EmailNotification> emailNotifications,
        List<SmsNotification> smsNotifications,
        CancellationToken ct
    );
    Task<NotificationReference> ProcessNotificationOrder(EmailNotification emailNotification, CancellationToken ct);
    Task<NotificationReference> ProcessNotificationOrder(SmsNotification smsNotification, CancellationToken ct);
}
