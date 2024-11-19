using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents a notification connected to a specific correspondence
/// </summary>
public sealed record CorrespondenceNotificationOrderResponse
{
    /// <summary>
    /// The id of the notification order
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The senders reference of the notification
    /// </summary>
    [JsonPropertyName("sendersReference")]
    public string? SendersReference { get; set; }

    /// <summary>
    /// The requested send time of the notification
    /// </summary>
    [JsonPropertyName("requestedSendTime")]
    public DateTime RequestedSendTime { get; set; }

    /// <summary>
    /// The short name of the creator of the notification order
    /// </summary>
    [JsonPropertyName("creator")]
    public required string Creator { get; init; }

    /// <summary>
    /// The date and time of when the notification order was created
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime Created { get; init; }

    /// <summary>
    /// Indicates if the notification is a reminder notification
    /// </summary>
    [JsonPropertyName("isReminder")]
    public bool IsReminder { get; init; }

    /// <summary>
    /// The preferred notification channel of the notification order
    /// </summary>
    [JsonPropertyName("notificationChannel")]
    public CorrespondenceNotificationChannel NotificationChannel { get; init; }

    /// <summary>
    /// Indicates if notifications generated by this order should ignore KRR reservations
    /// </summary>
    [JsonPropertyName("ignoreReservation")]
    public bool? IgnoreReservation { get; init; }

    /// <summary>
    /// The id of the resource that this notification relates to
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; init; }

    /// <summary>
    /// The processing status of the notication order
    /// </summary>
    [JsonPropertyName("processingStatus")]
    public CorrespondenceNotificationStatusSummaryResponse? ProcessingStatus { get; init; }

    /// <summary>
    /// The summary of the notifications statuses
    /// </summary>
    [JsonPropertyName("notificationStatusDetails")]
    public CorrespondenceNotificationSummaryResponse? NotificationStatusDetails { get; init; }
}
