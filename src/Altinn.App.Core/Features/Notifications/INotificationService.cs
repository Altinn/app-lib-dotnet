using Altinn.App.Core.Models.Notifications;
using Altinn.App.Core.Models.Notifications.Future;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

internal interface INotificationService
{
    Task<List<NotificationReference>> NotifyInstanceOwnerOnInstansiation(
        string language,
        NotificationOrderRequest orderRequest,
        InstanceOwner instanceOwner,
        CancellationToken ct
    );
}
