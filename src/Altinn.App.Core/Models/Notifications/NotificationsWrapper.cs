using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;

namespace Altinn.App.Core.Models.Notifications;

internal readonly record struct NotificationsWrapper(
    List<EmailNotification>? EmailNotifications,
    List<SmsNotification>? SmsNotifications
);
