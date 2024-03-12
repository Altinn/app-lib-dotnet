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
    /// <returns>Boolean</returns>
    /// <param name="emailNotification">The email notification,</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> RequestEmailNotification(EmailNotification emailNotification, CancellationToken ct);
}
