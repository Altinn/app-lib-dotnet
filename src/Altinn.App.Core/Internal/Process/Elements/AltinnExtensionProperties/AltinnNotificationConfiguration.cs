using Altinn.App.Core.Features.Notifications;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

public class AltinnNotificationConfiguration
{
    /// Optionally set a notification provider that should be used for sending notifications related to this task.
    /// The notification provider with a matching ID must be registered as a transient service in the DI container.
    ///
    /// The provider must be an implementation of <see cref="INotificationProvider"/>
    public string? NotificationProviderId { get; set; }

    public SmsOverride? SmsOverride { get; set; }

    public EmailOverride? EmailOverride { get; set; }

    internal ValidAltinnNotificationConfiguration Validate()
    {
        //TODO: implement validation logic

        return new ValidAltinnNotificationConfiguration(NotificationProviderId, SmsOverride, EmailOverride);
    }
}

public class SmsOverride
{
    public string SenderNumber { get; set; } = string.Empty;
    public string BodyTextResource { get; set; } = string.Empty;
}

public class EmailOverride
{
    public string SubjectTextResource { get; set; } = string.Empty;

    public string BodyTextResource { get; set; } = string.Empty;
}

internal readonly record struct ValidAltinnNotificationConfiguration(
    string? NotificationProviderId,
    SmsOverride? SmsOverride,
    EmailOverride? EmailOverride
);
