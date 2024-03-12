using Altinn.App.Core.Infrastructure.Clients.Notification;

namespace Altinn.App.Core.Models.Email;
/// <summary>
/// Structure used by <see cref="EmailNotificationClient"/> to request an email notification to a list of recipients.
/// </summary>
public class EmailNotification
{
    /// <summary>
    /// The subject of the email.
    /// </summary>
    public string Subject { get; set; }
    /// <summary>
    /// The body of the email. 
    /// </summary>
    public string Body { get; set; }
    /// <summary>
    /// The content type of the email. 
    /// "Plain" by default.
    /// </summary>
    public string ContentType { get; set; }
    /// <summary>
    /// The Requested send time for the email. 
    /// DateTime.Now by default.
    /// </summary>
    public DateTime RequestedSendTime { get; set; }
    /// <summary>
    /// The senders reference for the email. 
    /// Used to track the email request.
    /// </summary>
    public string SendersReference { get; set; }
    /// <summary>
    /// The recipients of the email. 
    /// </summary>
    public List<EmailRecipient> Recipients { get; set; } = [];
    /// <summary>
    /// Structure used by <see cref="EmailNotificationClient"/> to request an email notification to a list of recipients.
    /// </summary>
    /// <param name="subject"><inheritdoc cref="Subject"/></param>
    /// <param name="body"><inheritdoc cref="Body"/></param>
    /// <param name="emailRecipients"><inheritdoc cref="Recipients"/></param>
    /// <param name="sendersReference"><inheritdoc cref="SendersReference"/></param>
    /// <param name="contentType"><inheritdoc cref="ContentType"/></param>
    /// <param name="requestedSendTime"><inheritdoc cref="RequestedSendTime"/></param>
    public EmailNotification(
        string subject, 
        string body, 
        List<EmailRecipient> emailRecipients, 
        string sendersReference, 
        string contentType = "Plain", 
        DateTime? requestedSendTime = null)
    {
        Subject = subject;
        Body = body;
        Recipients = emailRecipients;
        SendersReference = sendersReference;
        ContentType = contentType;
        RequestedSendTime = requestedSendTime is null ? DateTime.Now : (DateTime) requestedSendTime;
    }
}