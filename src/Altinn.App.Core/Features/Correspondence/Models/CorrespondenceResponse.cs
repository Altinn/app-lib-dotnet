using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Response from the Altinn Correspondence server after a successful request
/// </summary>
/// <param name="Correspondences">The correspondences that were processed</param>
/// <param name="AttachmentIds">The attachments linked to the correspondence</param>
public sealed record CorrespondenceResponse(List<CorrespondenceDetails> Correspondences, List<string> AttachmentIds);

/// <summary>
/// Details about the correspondence
/// </summary>
public sealed record CorrespondenceDetails
{
    /// <summary>
    /// The correspondence identifier
    /// </summary>
    public Guid CorrespondenceId { get; init; }

    /// <summary>
    /// The status of the correspondence
    /// </summary>
    public CorrespondenceStatus Status { get; init; }

    /// <summary>
    /// The recipient of the correspondence
    /// </summary>
    [OrganisationNumberJsonConverter(OrganisationNumberFormat.International)]
    public OrganisationNumber Recipient { get; init; }

    /// <summary>
    /// Notifications linked to the correspondence
    /// </summary>
    public List<CorrespondenceNotificationDetails>? Notifications { get; init; }
}

/// <summary>
/// Details about the correspondence notification
/// </summary>
/// <param name="OrderId">The notification order identifier</param>
/// <param name="IsReminder">Whether or not this is a reminder notification</param>
/// <param name="Status">The status of the notification</param>
public sealed record CorrespondenceNotificationDetails(
    Guid? OrderId,
    bool? IsReminder,
    CorrespondenceNotificationStatus? Status
);

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
