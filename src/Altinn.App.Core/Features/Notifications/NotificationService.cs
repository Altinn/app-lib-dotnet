using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Future;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Notifications;

internal sealed class NotificationService : INotificationService
{
    private readonly INotificationOrderClient _notificationOrderClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationOrderClient notificationOrderClient,
        ILogger<NotificationService> logger
    )
    {
        _notificationOrderClient = notificationOrderClient;
        _logger = logger;
    }

    public async Task<List<NotificationReference>> NotifyInstanceOwnerOnInstansiation(
        string language,
        NotificationOrderRequest orderRequest,
        InstanceOwner instanceOwner,
        CancellationToken ct
    )
    {
        List<NotificationReference> notificationReferences = [];

        NotificationOrderResponse response = await _notificationOrderClient.Order(orderRequest, ct);

        notificationReferences.Add(new NotificationReference(response.Notification.ShipmentId.ToString(), response.Notification.SendersReference));

        foreach (NotificationOrderShipment notificationReference in response.Reminders)
        {
            var reference = new NotificationReference(notificationReference.ShipmentId.ToString(), notificationReference.SendersReference);
            notificationReferences.Add(reference);
        }

        return notificationReferences;
    }
}
