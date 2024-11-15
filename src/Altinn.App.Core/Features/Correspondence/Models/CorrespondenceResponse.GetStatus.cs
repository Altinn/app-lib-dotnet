using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

partial class CorrespondenceResponse
{
    /// <summary>
    /// Response after a successful <see cref="CorrespondenceClient.GetStatus"/> request
    /// </summary>
    public sealed record GetStatus
    {
        /// <summary>
        /// The status history for the corrrespondence
        /// </summary>
        [JsonPropertyName("statusHistory")]
        public required IEnumerable<StatusEvent> StatusHistory { get; init; }

        /// <summary>
        /// Notifications directly related to this correspondence
        /// </summary>
        [JsonPropertyName("notifications")]
        public IEnumerable<NotificationOrder>? Notifications { get; init; }

        /// <summary>
        /// The recipient of the correspondence. Either an organisation number or identity number
        /// </summary>
        [JsonPropertyName("recipient")]
        public required string Recipient { get; init; }

        /// <summary>
        /// Indicates if the correspondence has been set as unread by the recipient
        /// </summary>
        [JsonPropertyName("markedUnread")]
        public bool? MarkedUnread { get; init; }

        /// <summary>
        /// Unique Id for this correspondence
        /// </summary>
        [JsonPropertyName("correspondenceId")]
        public Guid CorrespondenceId { get; init; }

        /// <summary>
        /// The correspondence content. Contains information about the correspondence body, subject etc.
        /// </summary>
        [JsonPropertyName("content")]
        public CorrespondenceContent? Content { get; init; }

        /// <summary>
        /// When the correspondence was created
        /// </summary>
        [JsonPropertyName("created")]
        public DateTimeOffset Created { get; init; }

        /// <summary>
        /// The current status for the correspondence
        /// </summary>
        [JsonPropertyName("status")]
        public CorrespondenceStatus Status { get; init; }

        /// <summary>
        /// The current status text for the correspondence
        /// </summary>
        [JsonPropertyName("statusText")]
        public string? StatusText { get; init; }

        /// <summary>
        /// Timestamp for when the current correspondence status was changed
        /// </summary>
        [JsonPropertyName("statusChanged")]
        public DateTimeOffset StatusChanged { get; init; }

        /// <summary>
        /// The resource id for the correspondence service
        /// </summary>
        [JsonPropertyName("resourceId")]
        public required string ResourceId { get; init; }

        /// <summary>
        /// The sending organisation of the correspondence
        /// </summary>
        [JsonPropertyName("sender")]
        [OrganisationNumberJsonConverter(OrganisationNumberFormat.International)]
        public OrganisationNumber Sender { get; init; }

        /// <summary>
        /// A reference value given to the message by the creator
        /// </summary>
        [JsonPropertyName("sendersReference")]
        public required string SendersReference { get; init; }

        /// <summary>
        /// An alternative name for the sender of the correspondence. The name will be displayed instead of the organization name
        ///  </summary>
        [JsonPropertyName("messageSender")]
        public string? MessageSender { get; init; }

        /// <summary>
        /// When the correspondence should become visible to the recipient
        /// </summary>
        [JsonPropertyName("RequestedPublishTime")]
        public DateTimeOffset? RequestedPublishTime { get; init; }

        /// <summary>
        /// The date for when Altinn can remove the correspondence from its database
        /// </summary>
        [JsonPropertyName("allowSystemDeleteAfter")]
        public DateTimeOffset? AllowSystemDeleteAfter { get; init; }

        /// <summary>
        /// A date and time for when the recipient must reply
        /// </summary>
        [JsonPropertyName("dueDateTime")]
        public DateTimeOffset DueDateTime { get; init; }

        /// <summary>
        /// Reference to other items in the Altinn ecosystem
        /// </summary>
        [JsonPropertyName("externalReferences")]
        public IEnumerable<ExternalReference>? ExternalReferences { get; init; }

        /// <summary>
        /// User-defined properties related to the correspondence
        /// </summary>
        [JsonPropertyName("propertyList")]
        public IReadOnlyDictionary<string, string>? PropertyList { get; init; }

        /// <summary>
        /// Options for how the recipient can reply to the correspondence
        /// </summary>
        [JsonPropertyName("replyOptions")]
        public IEnumerable<CorrespondenceReplyOption>? ReplyOptions { get; init; }

        /// <summary>
        /// Specifies whether the correspondence can override reservation against digital comminication in KRR
        /// </summary>
        [JsonPropertyName("IgnoreReservation")]
        public bool? IgnoreReservation { get; init; }

        /// <summary>
        /// The time the correspondence was published
        /// </summary>
        /// <remarks>
        /// A null value means the correspondence has not yet been published
        /// </remarks>
        [JsonPropertyName("Published")]
        public DateTimeOffset? Published { get; init; }

        /// <summary>
        /// Specifies whether reading the correspondence needs to be confirmed by the recipient
        /// </summary>
        [JsonPropertyName("IsConfirmationNeeded")]
        public bool IsConfirmationNeeded { get; set; }

        /// <summary>
        /// Represents a correspondence status event
        /// </summary>
        public sealed record StatusEvent
        {
            /// <summary>
            /// The event status indicator
            /// </summary>
            [JsonPropertyName("status")]
            public CorrespondenceStatus Status { get; init; }

            /// <summary>
            /// Description of the status
            /// </summary>
            [JsonPropertyName("statusText")]
            public required string StatusText { get; init; }

            /// <summary>
            /// Timestamp for when this correspondence status event occurred
            /// </summary>
            [JsonPropertyName("statusChanged")]
            public DateTimeOffset StatusChanged { get; init; }
        }

        /// <summary>
        /// Represents a notification connected to a specific correspondence
        /// </summary>
        public sealed record NotificationOrder
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
            public NotificationStatusSummary? ProcessingStatus { get; init; }

            /// <summary>
            /// The summary of the notifications statuses
            /// </summary>
            [JsonPropertyName("notificationStatusDetails")]
            public NotificationStatusDetails? NotificationStatusDetails { get; init; }
        }

        /// <summary>
        /// Represents a summary of status overviews from all notification channels
        /// </summary>
        public sealed record NotificationStatusDetails
        {
            /// <summary>
            /// Notifications sent via Email
            /// </summary>
            [JsonPropertyName("email")]
            public NotificationDetails? Email { get; init; }

            /// <summary>
            /// Notifications sent via SMS
            /// </summary>
            [JsonPropertyName("sms")]
            public NotificationDetails? Sms { get; init; }
        }

        /// <summary>
        /// Represents a status overview from a single notification channel
        /// </summary>
        public sealed record NotificationDetails
        {
            /// <summary>
            /// The notification id
            /// </summary>
            [JsonPropertyName("id")]
            public Guid Id { get; init; }

            /// <summary>
            /// Indicates if the sending of the notification was successful
            /// </summary>
            [JsonPropertyName("succeeded")]
            public bool Succeeded { get; init; }

            /// <summary>
            /// The recipient of the notification. Either an organisation number or identity number
            /// </summary>
            [JsonPropertyName("recipient")]
            public NotificationRecipient? Recipient { get; init; }

            /// <summary>
            /// The result status of the notification
            /// </summary>
            [JsonPropertyName("sendStatus")]
            public NotificationStatusSummary? SendStatus { get; init; }
        }

        /// <summary>
        /// Represents a recipient of a notification
        /// </summary>
        public sealed record NotificationRecipient
        {
            /// <summary>
            /// The email address of the recipient
            /// </summary>
            [JsonPropertyName("emailAddress")]
            public string? EmailAddress { get; init; }

            /// <summary>
            /// The mobile phone number of the recipient
            /// </summary>
            [JsonPropertyName("mobileNumber")]
            public string? MobileNumber { get; init; }

            /// <summary>
            /// The organization number of the recipient
            /// </summary>
            [JsonPropertyName("organizationNumber")]
            public string? OrganizationNumber { get; init; }

            /// <summary>
            /// The SSN/identity number of the recipient
            /// </summary>
            [JsonPropertyName("nationalIdentityNumber")]
            public string? NationalIdentityNumber { get; init; }

            /// <summary>
            /// Indicates if the recipient is reserved from receiving communication
            /// </summary>
            [JsonPropertyName("isReserved")]
            public bool? IsReserved { get; init; }
        }

        /// <summary>
        /// Represents the status summary of a notification
        /// </summary>
        public sealed record NotificationStatusSummary
        {
            /// <summary>
            /// The status
            /// </summary>
            [JsonPropertyName("status")]
            public required string Status { get; init; }

            /// <summary>
            /// The status description
            /// </summary>
            [JsonPropertyName("description")]
            public string? Description { get; init; }

            /// <summary>
            /// The date and time of when the status was last updated
            /// </summary>
            [JsonPropertyName("lastUpdate")]
            public DateTime LastUpdate { get; init; }
        }

        /// <summary>
        /// Represents the content of a correspondence
        /// </summary>
        public sealed record CorrespondenceContent
        {
            /// <summary>
            /// The language of the correspondence, specified according to ISO 639-1
            /// </summary>
            [JsonPropertyName("language")]
            [JsonConverter(typeof(LanguageCodeConverter<Iso6391>))]
            public LanguageCode<Iso6391> Language { get; init; }

            /// <summary>
            /// The correspondence message title (subject)
            /// </summary>
            [JsonPropertyName("messageTitle")]
            public required string MessageTitle { get; init; }

            /// <summary>
            /// The summary text of the correspondence
            /// </summary>
            [JsonPropertyName("messageSummary")]
            public required string MessageSummary { get; init; }

            /// <summary>
            /// The main body of the correspondence
            /// </summary>
            [JsonPropertyName("messageBody")]
            public required string MessageBody { get; init; }

            /// <summary>
            /// A list of attachments for the correspondence
            /// </summary>
            [JsonPropertyName("attachments")]
            public IEnumerable<CorrespondenceAttachment>? Attachments { get; init; }
        }

        /// <summary>
        /// Represents a binary attachment to a Correspondence
        /// </summary>
        public sealed record CorrespondenceAttachment
        {
            /// <summary>
            /// A unique id for the correspondence attachment
            /// </summary>
            [JsonPropertyName("id")]
            public Guid Id { get; init; }

            /// <summary>
            /// The date and time when the attachment was created
            /// </summary>
            [JsonPropertyName("created")]
            public DateTimeOffset Created { get; init; }

            /// <summary>
            /// The location of the attachment data
            /// </summary>
            [JsonPropertyName("dataLocationType")]
            public AttachmentDataLocationType DataLocationType { get; init; }

            /// <summary>
            /// The current status of the attachment
            /// </summary>
            [JsonPropertyName("status")]
            public AttachmentStatus Status { get; init; }

            /// <summary>
            /// The text description of the status code
            /// </summary>
            [JsonPropertyName("statusText")]
            public required string StatusText { get; init; }

            /// <summary>
            /// The date and time when the current attachment status was changed
            /// </summary>
            [JsonPropertyName("StatusChanged")]
            public DateTimeOffset StatusChanged { get; init; }

            /// <summary>
            /// The date and time when the attachment expires
            /// </summary>
            [JsonPropertyName("expirationTime")]
            public DateTimeOffset ExpirationTime { get; init; }

            /// <summary>
            /// The filename of the attachment
            /// </summary>
            [JsonPropertyName("fileName")]
            public string? FileName { get; init; }

            /// <summary>
            /// The display name of the attachment
            /// </summary>
            [JsonPropertyName("name")]
            public required string Name { get; init; }

            /// <summary>
            /// The name of the restriction policy restricting access to this element
            /// </summary>
            /// <remarks>
            /// An empty value indicates no restriction above the ones governing the correspondence referencing this attachment
            /// </remarks>
            [JsonPropertyName("restrictionName")]
            public string? RestrictionName { get; init; }

            /// <summary>
            /// Indicates if the attachment is encrypted or not
            /// </summary>
            [JsonPropertyName("isEncrypted")]
            public bool IsEncrypted { get; init; }

            /// <summary>
            /// MD5 checksum of the file data
            /// </summary>
            [JsonPropertyName("checksum")]
            public string? Checksum { get; init; }

            /// <summary>
            /// A reference value given to the attachment by the creator
            /// </summary>
            [JsonPropertyName("sendersReference")]
            public required string SendersReference { get; init; }

            /// <summary>
            /// The attachment data type in MIME format
            /// </summary>
            [JsonPropertyName("dataType")]
            public required string DataType { get; init; }
        }

        /// <summary>
        /// Represents a reference to another item in the Altinn ecosystem
        /// </summary>
        public sealed record ExternalReference
        {
            /// <summary>
            /// The reference Value
            /// </summary>
            [JsonPropertyName("referenceValue")]
            public required string ReferenceValue { get; init; }

            /// <summary>
            /// The Type of reference
            /// </summary>
            [JsonPropertyName("referenceType")]
            public required CorrespondenceReferenceType ReferenceType { get; init; }
        }

        /// <summary>
        /// Defines the location of the attachment data
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum AttachmentDataLocationType
        {
            /// <summary>
            /// Specifies that the attachment data is stored in the Altinn correspondence storage
            /// </summary>
            AltinnCorrespondenceAttachment,

            /// <summary>
            /// Specifies that the attachment data is stored in an external storage controlled by the sender
            /// </summary>
            ExternalStorage
        }

        /// <summary>
        /// Represents the status of an attachment
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum AttachmentStatus
        {
            /// <summary>
            /// Attachment has been Initialized.
            /// </summary>
            Initialized,

            /// <summary>
            /// Awaiting processing of upload
            /// </summary>
            UploadProcessing,

            /// <summary>
            /// Published and available for download
            /// </summary>
            Published,

            /// <summary>
            /// Purged
            /// </summary>
            Purged,

            /// <summary>
            /// Failed
            /// </summary>
            Failed
        }
    }
}
