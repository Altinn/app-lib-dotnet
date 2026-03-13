using Altinn.App.Core.Models.Notifications;

namespace Altinn.App.Core.Features.Notifications;

internal interface INotificationReader
{
    /// <summary>
    /// Gets the notifications for the current task.
    /// </summary>
    NotificationsWrapper GetProvidedNotifications(string notificationProviderId, CancellationToken ct);
}
