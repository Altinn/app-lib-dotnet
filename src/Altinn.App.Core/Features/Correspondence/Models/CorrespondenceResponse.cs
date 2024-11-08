using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Response from the Altinn Correspondence server after a successful request
/// </summary>
public sealed record CorrespondenceResponse
{
    /// <summary>
    /// The correspondences that were processed
    /// </summary>
    [JsonPropertyName("correspondences")]
    public required List<CorrespondenceDetails> Correspondences { get; init; }

    /// <summary>
    /// The attachments linked to the correspondence
    /// </summary>
    [JsonPropertyName("attachmentIds")]
    public List<Guid>? AttachmentIds { get; init; }
}

/// <summary>
/// Details about the correspondence
/// </summary>
public sealed record CorrespondenceDetails
{
    /// <summary>
    /// The correspondence identifier
    /// </summary>
    [JsonPropertyName("correspondenceId")]
    public required Guid CorrespondenceId { get; init; }

    /// <summary>
    /// The status of the correspondence
    /// </summary>
    [JsonPropertyName("status")]
    public required CorrespondenceStatus Status { get; init; }

    /// <summary>
    /// The recipient of the correspondence
    /// </summary>
    [JsonPropertyName("recipient")]
    [OrganisationNumberJsonConverter(OrganisationNumberFormat.International)]
    public required OrganisationNumber Recipient { get; init; }

    /// <summary>
    /// Notifications linked to the correspondence
    /// </summary>
    [JsonPropertyName("notifications")]
    public List<CorrespondenceNotificationDetails>? Notifications { get; init; }
}

/// <summary>
/// Details about the correspondence notification
/// </summary>
public sealed record CorrespondenceNotificationDetails
{
    /// <summary>
    /// The notification order identifier
    /// </summary>
    [JsonPropertyName("orderId")]
    public required Guid OrderId { get; init; }

    /// <summary>
    /// Whether or not this is a reminder notification
    /// </summary>
    [JsonPropertyName("isReminder")]
    public required bool IsReminder { get; init; }

    /// <summary>
    /// The status of the notification
    /// </summary>
    [JsonPropertyName("status")]
    public required CorrespondenceNotificationStatus Status { get; init; }
}

/// <summary>
/// The status of the correspondence notification
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CorrespondenceNotificationStatus
{
    /// <summary>
    /// Notification has been scheduled successfully
    /// </summary>
    Success,

    /// <summary>
    /// Notification cannot be delivered because of missing contact information
    /// </summary>
    MissingContact,

    /// <summary>
    /// Notification has failed
    /// </summary>
    Failure
}

/// <summary>
/// The status of the correspondence
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CorrespondenceStatus
{
    /// <summary>
    /// Correspondence has been Initialized
    /// </summary>
    Initialized,

    /// <summary>
    /// Correspondence is ready for publish, but not available for recipient
    /// </summary>
    ReadyForPublish,

    /// <summary>
    /// Correspondence has been published, and is available for recipient
    /// </summary>
    Published,

    /// <summary>
    /// Correspondence fetched by recipient
    /// </summary>
    Fetched,

    /// <summary>
    /// Correspondence read by recipient
    /// </summary>
    Read,

    /// <summary>
    /// Recipient has replied to the correspondence
    /// </summary>
    Replied,

    /// <summary>
    /// Correspondence confirmed by recipient
    /// </summary>
    Confirmed,

    /// <summary>
    /// Correspondence has been purged by recipient
    /// </summary>
    PurgedByRecipient,

    /// <summary>
    /// Correspondence has been purged by Altinn
    /// </summary>
    PurgedByAltinn,

    /// <summary>
    /// Correspondence has been archived
    /// </summary>
    Archived,

    /// <summary>
    /// Recipient has opted out of digital communication in KRR
    /// </summary>
    Reserved,

    /// <summary>
    /// Correspondence has failed
    /// </summary>
    Failed
}
