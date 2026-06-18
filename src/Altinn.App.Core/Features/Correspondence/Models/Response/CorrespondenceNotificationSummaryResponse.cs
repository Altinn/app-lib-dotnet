using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents a summary of status overviews from all notification channels.
/// </summary>
public sealed record CorrespondenceNotificationSummaryResponse
{
    /// <summary>
    /// Notification sent via email.
    /// </summary>
    /// <remarks>
    /// When a notification is sent to multiple recipients, see <see cref="Emails"/> for the full set.
    /// </remarks>
    [JsonPropertyName("email")]
    public CorrespondenceNotificationStatusDetailsResponse? Email { get; init; }

    /// <summary>
    /// Notification sent via SMS.
    /// </summary>
    /// <remarks>
    /// When a notification is sent to multiple recipients, see <see cref="Smses"/> for the full set.
    /// </remarks>
    [JsonPropertyName("sms")]
    public CorrespondenceNotificationStatusDetailsResponse? Sms { get; init; }

    /// <summary>
    /// Notifications sent via email, in case the notification was sent to multiple recipients.
    /// </summary>
    [JsonPropertyName("emails")]
    public IReadOnlyList<CorrespondenceNotificationStatusDetailsResponse>? Emails { get; init; }

    /// <summary>
    /// Notifications sent via SMS, in case the notification was sent to multiple recipients.
    /// </summary>
    [JsonPropertyName("smses")]
    public IReadOnlyList<CorrespondenceNotificationStatusDetailsResponse>? Smses { get; init; }
}
