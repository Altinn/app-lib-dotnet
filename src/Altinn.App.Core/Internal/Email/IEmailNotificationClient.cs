using Altinn.App.Core.Models.Email;

namespace Altinn.App.Core.Internal.Email;

/// <summary>
/// Defines the required operations on a client of the Email notification service.
/// </summary>
public interface IEmailNotificationClient
{
    /// <summary>
    /// Requests an email notification.
    /// </summary>
    /// <returns>The id of the email notification order</returns>
    Task<string> RequestEmailNotification(string url, EmailNotification emailNotification, CancellationToken ct);
}
