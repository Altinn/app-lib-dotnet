using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Models.Notifications;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationReader : INotificationReader
{
    private readonly AppImplementationFactory _appImplementationFactory;

    public NotificationReader(AppImplementationFactory appImplementationFactory)
    {
        _appImplementationFactory = appImplementationFactory;
    }

    public NotificationsWrapper GetProvidedNotifications(string notificationProviderId, CancellationToken ct)
    {
        INotificationProvider provider;
        try
        {
            provider = _appImplementationFactory
                .GetAll<INotificationProvider>()
                .Single(x => x.Id == notificationProviderId);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConfigurationException(
                $"The notification provider id did not match an implementation of {nameof(INotificationProvider)}",
                ex
            );
        }

        var notificationsWrapper = new NotificationsWrapper(
            provider.ProvidedEmailNotifications,
            provider.ProvidedSmsNotifications
        );
        return notificationsWrapper;
    }
}
