using Altinn.App.Core.Internal.Email;
using Altinn.App.Core.Models.Email;

namespace Altinn.App.Core.Infrastructure.Clients.Notification;
/// <summary>
/// Implementation of the <see cref="IEmailNotificationClient"/> interface using a HttpClient to send
/// requests to the Email Notification service.
/// </summary>
public class EmailNotificationClient : IEmailNotificationClient
{
    /// <inheritdoc/>
    public Task<bool> RequestEmailNotification(EmailNotification emailNotification, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
