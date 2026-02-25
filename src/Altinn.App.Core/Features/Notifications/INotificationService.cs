using Altinn.App.Core.Models.Notifications.Future;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Notifications;

/// <summary>
/// Interface for handling notifications related to instances.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends notifications to the instance owner related to the instansiation of the instance.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="instansiationNotification"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task NotifyInstanceOwnerOnInstansiation(
        Instance instance,
        Party party,
        InstansiationNotification instansiationNotification,
        CancellationToken ct
    );
}
