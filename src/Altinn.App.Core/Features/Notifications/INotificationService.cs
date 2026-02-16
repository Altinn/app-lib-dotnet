using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

internal interface INotificationService
{
    Task<List<NotificationReference>> ProcessNotificationOrders(
        List<EmailNotification> emailNotifications,
        List<SmsNotification> smsNotifications,
        CancellationToken ct
    );
    Task<List<NotificationReference>> NotifyInstanceOwner(
        Instance instance,
        EmailOverride? emailOverride,
        SmsOverride? smsOverride,
        CancellationToken ct
    );
}
