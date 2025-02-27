using Altinn.App.Core.Features.Signing.Enums;
using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Helpers;

internal static class SigningCorrespondenceHelper
{
    /// <summary>
    /// Gets the notification choice.
    /// </summary>
    /// <param name="notification"></param>
    /// <returns>The notification choice as a <see cref="NotificationChoice"/>.</returns>
    internal static NotificationChoice GetNotificationChoice(Notification? notification)
    {
        if (notification is null)
        {
            return NotificationChoice.None;
        }

        if (notification.Email is null && notification.Sms is null)
        {
            return NotificationChoice.None;
        }

        if (notification.Email is not null && notification.Sms is not null)
        {
            return NotificationChoice.SmsAndEmail;
        }

        if (notification.Email is not null)
        {
            return NotificationChoice.Email;
        }

        return NotificationChoice.Sms;
    }
}
