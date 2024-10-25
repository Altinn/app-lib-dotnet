namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Available notification channels (methods)
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// The selected channel for the notification is email.
    /// </summary>
    Email,

    /// <summary>
    /// The selected channel for the notification is sms.
    /// </summary>
    Sms,

    /// <summary>
    /// The selected channel for the notification is email preferred.
    /// </summary>
    EmailPreferred,

    /// <summary>
    /// The selected channel for the notification is SMS preferred.
    /// </summary>
    SmsPreferred
}
