using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Notifications.Order;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.End;

internal sealed class CancelNotificationsProcessEnd : IProcessEnd
{
    private readonly INotificationCancelClient _notificationCancelClient;

    public CancelNotificationsProcessEnd(INotificationCancelClient notificationCancelClient)
    {
        _notificationCancelClient = notificationCancelClient;
    }
    public async Task End(Instance instance, List<InstanceEvent>? events)
    {
        // TODO: Fetch notification orders
        List<string> notificationOrderIds = [];

        foreach (string notificationOrderId in notificationOrderIds)
        {
            try
            {
                Guid orderGuid = Guid.Parse(notificationOrderId);
                await _notificationCancelClient.Cancel(orderGuid, CancellationToken.None);
            }
            catch (NotificationCancelException e)
            {
                // Log and swallow exception, we don't want to fail the entire process end if cancelling notifications fails
            }
            catch (FormatException e)
            {
                // Log and swallow exception, the notification order id was not in a valid format
            }
        }
    }
}
