using Altinn.App.Core.Models.Notifications.Sms;

namespace Altinn.App.Core.Internal.Notifications.Sms;

/// <summary>
/// Client for managing Altinn SMS notifications
/// </summary>
public interface ISmsNotificationClient
{
    /// <summary>
    /// Orders a new SMS notification
    /// </summary>
    /// <param name="smsNotification"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="SmsNotificationException"></exception>
    Task<SmsNotificationOrderResponse> Order(SmsNotification smsNotification, CancellationToken ct);
}
