using System.Xml.Serialization;
using Altinn.App.Core.Features.Notifications;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for notifications in a process task.
/// </summary>
public sealed class AltinnNotificationConfiguration
{
    /// Optionally set a notification provider that should be used for sending notifications related to this task.
    /// The notification provider with a matching ID must be registered as a transient service in the DI container.
    ///
    /// The provider must be an implementation of <see cref="INotificationProvider"/>
    [XmlElement("notificationProviderId", Namespace = "http://altinn.no/process")]
    public string? NotificationProviderId { get; set; }

    /// <summary>
    /// Configuration for sending SMS notifications. If not set, no SMS notifications will be sent.
    /// </summary>
    [XmlElement("smsConfig", Namespace = "http://altinn.no/process")]
    public SmsConfig? SmsConfig { get; set; }


    /// <summary>
    /// Configuration for sending email notifications. If not set, no email notifications will be sent.
    /// </summary>
    [XmlElement("emailConfig", Namespace = "http://altinn.no/process")]
    public EmailConfig? EmailConfig { get; set; }

    internal ValidAltinnNotificationConfiguration Validate()
    {
        //TODO: implement validation logic

        return new ValidAltinnNotificationConfiguration(NotificationProviderId, SmsConfig, EmailConfig);
    }
}

/// <summary>
/// Configuration for sending SMS notifications
/// </summary>
public class SmsConfig
{
    /// <summary>
    /// Indicates whether an SMS should be sent or not. False by default.
    /// </summary>
    [XmlAttribute("sendSms")]
    public bool SendSms { get; set; } = false;

    /// <summary>
    /// The senders number to be used when sending the SMS.
    /// </summary>
    [XmlElement("senderNumber", Namespace = "http://altinn.no/process")]
    public string SenderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Text resource ID for the body of the SMS.
    /// </summary>
    [XmlElement("bodyTextResource", Namespace = "http://altinn.no/process")]
    public string BodyTextResource { get; set; } = string.Empty;
}

/// <summary>
/// Configuration for sending email notifications
/// </summary>
public class EmailConfig
{
    /// <summary>
    /// Indicates whether an email should be sent or not. False by default.
    /// </summary>
    [XmlAttribute("sendEmail")]
    public bool SendEmail { get; set; } = false;

    /// <summary>
    /// Text resource ID for the subject of the email.
    /// </summary>
    [XmlElement("subjectTextResource", Namespace = "http://altinn.no/process")]
    public string SubjectTextResource { get; set; } = string.Empty;

    /// <summary>
    /// Text resource ID for the body of the email.
    /// </summary>
    [XmlElement("bodyTextResource", Namespace = "http://altinn.no/process")]
    public string BodyTextResource { get; set; } = string.Empty;
}

internal readonly record struct ValidAltinnNotificationConfiguration(
    string? NotificationProviderId,
    SmsConfig? SmsConfig,
    EmailConfig? EmailConfig
);
