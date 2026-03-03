namespace Altinn.App.Core.Models.Notifications;

/// <summary>
/// Represents a reference to a notification order, which can be used to track the status of the notification.
/// </summary>
/// <param name="SendersReference">The reference provided by the sender of the notification, which can be used to correlate the notification with the sender's own records.</param>
/// <param name="OrderId">The unique identifier for the notification order, which can be used to track the status of the notification in the notification system.</param>
public sealed record NotificationReference(string SendersReference, string OrderId);
