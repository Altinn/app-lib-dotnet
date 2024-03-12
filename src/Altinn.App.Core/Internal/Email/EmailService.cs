using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Email;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Email;
public class EmailService : IEmailService
{
    private readonly IEmailNotificationClient _emailNotificationClient;
    private readonly GeneralSettings _generalSettings;

    public Task<string> SendEmail(Instance instance, string taskId, CancellationToken ct)
    {
        var baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance));
        // TODO: Get the email data from the instance
        List<EmailRecipient> recipients = [];
        var emailNotif = new EmailNotification(subject: "", body: "", sendersReference: "", emailRecipients: recipients);

        return _emailNotificationClient.RequestEmailNotification(baseUrl, emailNotif, ct);
    }
    public EmailService(IEmailNotificationClient emailNotificationClient, IOptions<GeneralSettings> generalSettings)
    {
        _emailNotificationClient = emailNotificationClient;
        _generalSettings = generalSettings.Value;
    }
}

public interface IEmailService
{
    Task<string> SendEmail(Instance instance, string taskId, CancellationToken ct);
}
