using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Notifications;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

public class AltinnNotificationConfiguration
{
    /// Optionally set a notification provider that should be used for sending notifications related to this task.
    /// The notification provider with a matching ID must be registered as a transient service in the DI container.
    ///
    /// The provider must be an implementation of <see cref="INotificationProvider"/>
    public string? NotificationProviderId { get; set; }

    internal ValidAltinnNotificationConfiguration Validate()
    {
        //TODO: implement validation logic

        return new ValidAltinnNotificationConfiguration(NotificationProviderId);
    }
}

internal readonly record struct ValidAltinnNotificationConfiguration(string? NotificationProviderId);
