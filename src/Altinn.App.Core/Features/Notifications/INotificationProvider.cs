using Altinn.App.Core.Models.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Sms;

namespace Altinn.App.Core.Features.Notifications;

/// <summary>
/// Interface for implementing app-specific logic for deriving notifications.
/// </summary>
[ImplementableByApps]
public interface INotificationProvider
{
    /// <summary>
    /// Used to select the correct <see cref="INotificationProvider" /> implementation for a given notification task. Should match the NotificationProviderId parameter in the task configuration.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Returns the email notification to be sent for the given notification task.
    /// </summary>
    public List<EmailNotification> ProvidedEmailNotifications { get; }

    /// <summary>
    /// Returns the SMS notification to be sent for the given notification task.
    /// </summary>
    public List<SmsNotification> ProvidedSmsNotifications { get; }
}
