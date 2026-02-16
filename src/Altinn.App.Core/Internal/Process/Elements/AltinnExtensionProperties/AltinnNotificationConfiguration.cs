using System.Xml.Serialization;
using Altinn.App.Core.Features.Notifications;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

public class AltinnNotificationConfiguration
{
    /// Optionally set a notification provider that should be used for sending notifications related to this task.
    /// The notification provider with a matching ID must be registered as a transient service in the DI container.
    ///
    /// The provider must be an implementation of <see cref="INotificationProvider"/>
    public string? NotificationProviderId { get; set; }

    public SmsConfig? SmsConfig { get; set; }

    public EmailConfig? EmailConfig { get; set; }

    internal ValidAltinnNotificationConfiguration Validate()
    {
        //TODO: implement validation logic

        return new ValidAltinnNotificationConfiguration(NotificationProviderId, SmsConfig, EmailConfig);
    }
}

public class SmsConfig
{
    [XmlAttribute("SendSms")]
    public bool SendSms { get; set; } = false;

    public string SenderNumber { get; set; } = string.Empty;
    public string BodyTextResource { get; set; } = string.Empty;
}

public class EmailConfig
{
    [XmlAttribute("SendEmail")]
    public bool SendEmail { get; set; } = false;

    public string SubjectTextResource { get; set; } = string.Empty;
    public string BodyTextResource { get; set; } = string.Empty;
}

internal readonly record struct ValidAltinnNotificationConfiguration(
    string? NotificationProviderId,
    SmsConfig? SmsConfig,
    EmailConfig? EmailConfig
);
