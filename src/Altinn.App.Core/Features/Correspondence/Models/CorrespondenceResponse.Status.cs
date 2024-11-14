using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/*

{
       "statusHistory": [
           {
               "status": "Initialized",
               "statusText": "Initialized",
               "statusChanged": "2024-11-14T11:05:56.843628+00:00"
           },
           {
               "status": "ReadyForPublish",
               "statusText": "ReadyForPublish",
               "statusChanged": "2024-11-14T11:06:00.165998+00:00"
           },
           {
               "status": "Published",
               "statusText": "Published",
               "statusChanged": "2024-11-14T11:06:56.208705+00:00"
           }
       ],
       "notifications": [
           {
               "id": "598e8044-5ec4-43f9-8ce2-6a37c24cc7df",
               "sendersReference": "1234",
               "requestedSendTime": "2024-11-14T12:10:57.031351Z",
               "creator": "digdir",
               "created": "2024-11-14T11:05:57.237047Z",
               "isReminder": true,
               "notificationChannel": "EmailPreferred",
               "ignoreReservation": true,
               "resourceId": "apps-correspondence-integrasjon2",
               "processingStatus": {
                   "status": "Registered",
                   "description": "Order has been registered and is awaiting requested send time before processing.",
                   "lastUpdate": "2024-11-14T11:05:57.237047Z"
               },
               "notificationStatusDetails": {
                   "email": null,
                   "sms": null
               }
           },
           {
               "id": "7ab0ff62-8c5d-4a2e-8ad2-7e7236e847a4",
               "sendersReference": "1234",
               "requestedSendTime": "2024-11-14T11:10:57.031351Z",
               "creator": "digdir",
               "created": "2024-11-14T11:05:57.054356Z",
               "isReminder": false,
               "notificationChannel": "EmailPreferred",
               "ignoreReservation": true,
               "resourceId": "apps-correspondence-integrasjon2",
               "processingStatus": {
                   "status": "Completed",
                   "description": "Order processing is completed. All notifications have been generated.",
                   "lastUpdate": "2024-11-14T11:05:57.054356Z"
               },
               "notificationStatusDetails": {
                   "email": {
                       "id": "0dabcc5c-c3de-4636-922c-e7b351cdbbfa",
                       "succeeded": true,
                       "recipient": {
                           "emailAddress": "daniel.skovli@digdir.no",
                           "mobileNumber": null,
                           "organizationNumber": "213872702",
                           "nationalIdentityNumber": null,
                           "isReserved": null
                       },
                       "sendStatus": {
                           "status": "Succeeded",
                           "description": "The email has been accepted by the third party email service and will be sent shortly.",
                           "lastUpdate": "2024-11-14T11:10:12.693438Z"
                       }
                   },
                   "sms": null
               }
           }
       ],
       "recipient": "0192:213872702",
       "markedUnread": null,
       "correspondenceId": "94fa9dd9-734e-4712-9d49-4018aeb1a5dc",
       "content": {
           "attachments": [
               {
                   "created": "2024-11-14T11:05:56.843622+00:00",
                   "dataLocationType": "AltinnCorrespondenceAttachment",
                   "status": "Published",
                   "statusText": "Published",
                   "StatusChanged": "2024-11-14T11:06:00.102333+00:00",
                   "expirationTime": "0001-01-01T00:00:00+00:00",
                   "id": "a40fad32-dad1-442d-b4e1-2564d4561c07",
                   "fileName": "hello-world-3-1.pDf",
                   "name": "This is the PDF filename 🍕",
                   "isEncrypted": false,
                   "checksum": "27bb85ec3681e3cd1ed44a079f5fc501",
                   "sendersReference": "1234",
                   "dataType": "application/pdf"
               }
           ],
           "language": "en",
           "messageTitle": "Message without publish-time 👋🏻",
           "messageSummary": "This is the summary ✌️",
           "messageBody": "But current\n\nHere is a newline.\n\nHere are some emojis: 📎👴🏻👨🏼‍🍳🥰"
       },
       "created": "2024-11-14T11:05:56.575089+00:00",
       "status": "Published",
       "statusText": "Published",
       "statusChanged": "2024-11-14T11:06:56.208705+00:00",
       "resourceId": "apps-correspondence-integrasjon2",
       "sender": "0192:991825827",
       "sendersReference": "1234",
       "messageSender": "Daniel",
       "RequestedPublishTime": "2024-05-29T13:31:28.290518+00:00",
       "allowSystemDeleteAfter": "2025-05-29T13:31:28.290518+00:00",
       "dueDateTime": "2025-05-29T13:31:28.290518+00:00",
       "externalReferences": [
           {
               "referenceValue": "test",
               "referenceType": "AltinnBrokerFileTransfer"
           },
           {
               "referenceValue": "01932a59-edc3-7038-823e-cf46908cd83b",
               "referenceType": "DialogportenDialogId"
           }
       ],
       "propertyList": {
           "anim5": "string",
           "culpa_852": "string",
           "deserunt_12": "string"
       },
       "replyOptions": [
           {
               "linkURL": "www.dgidir.no",
               "linkText": "dgidir"
           },
           {
               "linkURL": "www.dgidir.no",
               "linkText": "dgidir"
           }
       ],
       "notification": null,
       "IgnoreReservation": true,
       "Published": "2024-11-14T11:06:56.208705+00:00",
       "IsConfirmationNeeded": false
   }

*/

partial class CorrespondenceResponse
{
    /// <summary>
    /// Response after a successful <see cref="CorrespondenceClient.Status"/> request
    /// </summary>
    public sealed record Status
    {
        /// <summary>
        /// The Status history for the Corrrespondence
        /// </summary>
        [JsonPropertyName("statusHistory")]
        public IEnumerable<StatusEvent>? StatusHistory { get; set; }

        /// <summary>
        /// Notifications directly related to this Correspondence
        /// </summary>
        [JsonPropertyName("notifications")]
        public IEnumerable<NotificationExt>? Notifications { get; set; }

        /// <summary>
        /// The recipient of the correspondence
        /// </summary>
        [JsonPropertyName("recipient")]
        public required string Recipient { get; set; }

        /// <summary>
        /// Indicates if the Correspondence has been set as unread by the recipient
        /// </summary>
        [JsonPropertyName("markedUnread")]
        public bool? MarkedUnread { get; set; }

        /// <summary>
        /// Unique Id for this correspondence
        /// </summary>
        [JsonPropertyName("correspondenceId")]
        public required Guid CorrespondenceId { get; set; }

        /// <summary>
        /// The correspondence content. Contains information about the Correspondence body, subject etc.
        /// </summary>
        [JsonPropertyName("content")]
        public CorrespondenceContent? Content { get; set; }

        /// <summary>
        /// When the correspondence was created
        /// </summary>
        [JsonPropertyName("created")]
        public required DateTimeOffset Created { get; set; }

        /// <summary>
        /// The current status for the Correspondence
        /// </summary>
        [JsonPropertyName("status")]
        public CorrespondenceStatus CorrespondenceStatus { get; set; }

        /// <summary>
        /// The current status text for the Correspondence
        /// </summary>
        [JsonPropertyName("statusText")]
        public string? StatusText { get; init; }

        /// <summary>
        /// Timestamp for when the Current Correspondence Status was changed
        /// </summary>
        [JsonPropertyName("statusChanged")]
        public DateTimeOffset StatusChanged { get; init; }

        /// <summary>
        /// Gets or sets the Resource Id for the correspondence service.
        /// </summary>
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; init; }

        /// <summary>
        /// The Sending organisation of the correspondence.
        /// </summary>
        /// <remarks>
        /// Organization number in countrycode:organizationnumber format.
        /// </remarks>
        [JsonPropertyName("sender")]
        public OrganisationNumber Sender { get; init; }

        /// <summary>
        /// Used by senders and receivers to identify specific a Correspondence using external identification methods.
        /// </summary>
        [JsonPropertyName("sendersReference")]
        public string? SendersReference { get; init; }

        /// <summary>
        /// An alternative name for the sender of the correspondence. The name will be displayed instead of the organization name.
        ///  </summary>
        [JsonPropertyName("messageSender")]
        public string? MessageSender { get; init; }

        /// <summary>
        /// When the correspondence should become visible to the recipient.
        /// </summary>
        [JsonPropertyName("RequestedPublishTime")]
        public DateTimeOffset? RequestedPublishTime { get; init; }

        /// <summary>
        /// Gets or sets the date for when Altinn can remove the correspondence from its database.
        /// </summary>
        [JsonPropertyName("allowSystemDeleteAfter")]
        public DateTimeOffset? AllowSystemDeleteAfter { get; init; }

        /// <summary>
        /// Gets or sets a date and time for when the recipient must reply.
        /// </summary>
        [JsonPropertyName("dueDateTime")]
        public DateTimeOffset? DueDateTime { get; init; }

        /// <summary>
        /// Gets or sets an list of references Senders can use this field to tell the recipient that the correspondence is related to the referenced item(s)
        /// Examples include Altinn App instances, Altinn Broker File Transfers
        /// </summary>
        /// <remarks>
        /// </remarks>
        [JsonPropertyName("externalReferences")]
        public IEnumerable<ExternalReference>? ExternalReferences { get; init; }

        /// <summary>
        /// User-defined properties related to the Correspondence
        /// </summary>
        [JsonPropertyName("propertyList")]
        public IReadOnlyDictionary<string, string>? PropertyList { get; init; }

        /// <summary>
        /// Options for how the recipient can reply to the correspondence
        /// </summary>
        [JsonPropertyName("replyOptions")]
        public IEnumerable<CorrespondenceReplyOption>? ReplyOptions { get; init; }

        /// <summary>
        /// Notifications directly related to this Correspondence.
        /// </summary>
        [JsonPropertyName("notification")]
        public CorrespondenceNotification? Notification { get; init; }

        /// <summary>
        /// Specifies whether the correspondence can override reservation against digital comminication in KRR
        /// </summary>
        [JsonPropertyName("IgnoreReservation")]
        public bool? IgnoreReservation { get; init; }

        /// <summary>
        /// Is null until the correspondence is published.
        /// </summary>
        [JsonPropertyName("Published")]
        public DateTimeOffset? Published { get; init; }

        /// <summary>
        /// Represents a Correspondence status event
        /// </summary>
        public sealed record StatusEvent
        {
            /// <summary>
            /// Correspondence Status Event
            /// </summary>
            [JsonPropertyName("status")]
            public CorrespondenceStatus? EventStatus { get; init; }

            /// <summary>
            /// Correspondence Status Text description
            /// </summary>
            [JsonPropertyName("statusText")]
            public string? StatusText { get; init; }

            /// <summary>
            /// Timestamp for when this Correspondence Status Event occurred
            /// </summary>
            [JsonPropertyName("statusChanged")]
            public DateTimeOffset? StatusChanged { get; init; }
        }

        /// <summary>
        /// Represents a notification connected to a specific correspondence
        /// </summary>
        public sealed record NotificationExt
        {
            /// <summary>
            /// Gets or sets the id of the notification order
            /// </summary>
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            /// <summary>
            /// Gets or sets the senders reference of the notification
            /// </summary>
            [JsonPropertyName("sendersReference")]
            public string? SendersReference { get; set; }

            /// <summary>
            /// Gets or sets the requested send time of the notification
            /// </summary>
            [JsonPropertyName("requestedSendTime")]
            public DateTime? RequestedSendTime { get; set; }

            /// <summary>
            /// Gets or sets the short name of the creator of the notification order
            /// </summary>
            [JsonPropertyName("creator")]
            public string? Creator { get; init; }

            /// <summary>
            /// Gets or sets the date and time of when the notification order was created
            /// </summary>
            [JsonPropertyName("created")]
            public DateTime? Created { get; init; }

            /// <summary>
            /// whether the notification is a reminder notification
            /// </summary>
            [JsonPropertyName("isReminder")]
            public bool? IsReminder { get; init; }

            /// <summary>
            /// Gets or sets the preferred notification channel of the notification order
            /// </summary>
            [JsonPropertyName("notificationChannel")]
            public CorrespondenceNotificationChannel? NotificationChannel { get; init; }

            /// <summary>
            /// Gets or sets whether notifications generated by this order should ignore KRR reservations
            /// </summary>
            [JsonPropertyName("ignoreReservation")]
            public bool? IgnoreReservation { get; init; }

            /// <summary>
            /// Gets or sets the id of the resource that the notification is related to
            /// </summary>
            [JsonPropertyName("resourceId")]
            public string? ResourceId { get; init; }

            /// <summary>
            /// Gets or sets the processing status of the notication order
            /// </summary>
            [JsonPropertyName("processingStatus")]
            public NotificationProcessingStatus? ProcessingStatus { get; init; }

            /// <summary>
            /// Gets or sets the summary of the notifiications statuses
            /// </summary>
            [JsonPropertyName("notificationStatusDetails")]
            public NotificationStatusDetails? NotificationStatusDetails { get; init; }
        }

        /// <summary>
        /// An abstrct  class representing a status overview of a notification channels
        /// </summary>
        public sealed record NotificationProcessingStatus
        {
            /// <summary>
            /// Gets or sets the status
            /// </summary>
            [JsonPropertyName("status")]
            public string? StatusSummary { get; init; }

            /// <summary>
            /// Gets or sets the description
            /// </summary>
            [JsonPropertyName("description")]
            public string? StatusDescription { get; init; }

            /// <summary>
            /// Gets or sets the date time of when the status was last updated
            /// </summary>
            [JsonPropertyName("lastUpdate")]
            public DateTime LastUpdate { get; init; }
        }

        /// <summary>
        /// A class representing a summary of status overviews of all notification channels
        /// </summary>
        public class NotificationStatusDetails
        {
            /// <summary>
            ///
            /// </summary>
            public NotificationDetails? Email { get; init; }

            /// <summary>
            ///
            /// </summary>
            public NotificationDetails? Sms { get; init; }
        }

        /// <summary>
        /// An abstrct  class representing a status overview of a notification channels
        /// </summary>
        public sealed record NotificationDetails
        {
            /// <summary>
            /// The notification id
            /// </summary>
            [JsonPropertyName("id")]
            public Guid Id { get; init; }

            /// <summary>
            /// Boolean indicating if the sending of the notification was successful
            /// </summary>
            [JsonPropertyName("succeeded")]
            public bool Succeeded { get; init; }

            /// <summary>
            /// The recipient of the notification
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
        /// A class representing a a recipient of a notification
        /// </summary>
        /// <remarks>
        /// External representaion to be used in the API.
        /// </remarks>
        public sealed record NotificationRecipient
        {
            /// <summary>
            /// the email address of the recipient
            /// </summary>
            public string? EmailAddress { get; init; }

            /// <summary>
            /// the mobileNumber of the recipient
            /// </summary>
            public string? MobileNumber { get; init; }

            /// <summary>
            /// the organization number of the recipient
            /// </summary>
            public string? OrganizationNumber { get; init; }

            /// <summary>
            /// The SSN of the recipient
            /// </summary>
            public string? NationalIdentityNumber { get; init; }

            /// <summary>
            /// Boolean indicating if the recipient is reserved
            /// </summary>
            public bool? IsReserved { get; init; }
        }

        /// <summary>
        /// A class representing a status summary
        /// </summary>
        /// <remarks>
        /// External representaion to be used in the API.
        /// </remarks>
        public sealed record NotificationStatusSummary
        {
            /// <summary>
            /// Gets or sets the status
            /// </summary>
            [JsonPropertyName("status")]
            public string? StatusSummary { get; init; }

            /// <summary>
            /// Gets or sets the description
            /// </summary>
            [JsonPropertyName("description")]
            public string? StatusDescription { get; init; }

            /// <summary>
            /// Gets or sets the date time of when the status was last updated
            /// </summary>
            [JsonPropertyName("lastUpdate")]
            public DateTime LastUpdate { get; init; }
        }

        /// <summary>
        /// Represents the content of a reportee element of the type correspondence.
        /// </summary>
        public sealed record CorrespondenceContent
        {
            /// <summary>
            /// Gets or sets the language of the correspondence, specified according to ISO 639-1
            /// </summary>
            [JsonPropertyName("language")]
            public string? Language { get; init; }

            /// <summary>
            /// Gets or sets the correspondence message title. Subject.
            /// </summary>
            [JsonPropertyName("messageTitle")]
            public string? MessageTitle { get; init; }

            /// <summary>
            /// Gets or sets a summary text of the correspondence.
            /// </summary>
            [JsonPropertyName("messageSummary")]
            public string? MessageSummary { get; init; }

            /// <summary>
            /// Gets or sets the main body of the correspondence.
            /// </summary>
            public string? MessageBody { get; init; }

            /// <summary>
            /// Gets or sets a list of attachments.
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
            /// A unique id for the correspondence attachment.
            /// </summary>
            [JsonPropertyName("id")]
            public Guid Id { get; init; }

            /// <summary>
            /// The date on which this attachment is created
            /// </summary>
            [JsonPropertyName("created")]
            public DateTimeOffset Created { get; init; }

            /// <summary>
            /// Specifies the location of the attachment data
            /// </summary>
            [JsonPropertyName("dataLocationType")]
            public AttachmentDataLocationType DataLocationType { get; init; }

            /// <summary>
            /// Current attachment status
            /// </summary>
            [JsonPropertyName("status")]
            public AttachmentStatus AttachmentStatus { get; init; }

            /// <summary>
            /// Current attachment status text description
            /// </summary>
            [JsonPropertyName("statusText")]
            public required string StatusText { get; init; }

            /// <summary>
            /// Timestamp for when the Current Attachment Status was changed
            /// </summary>
            [JsonPropertyName("StatusChanged")]
            public DateTimeOffset StatusChanged { get; init; }

            /// <summary>
            /// When the attachment expires
            /// </summary>
            [JsonPropertyName("expirationTime")]
            public DateTimeOffset ExpirationTime { get; init; }

            /// <summary>
            /// The name of the attachment file.
            /// </summary>
            [JsonPropertyName("fileName")]
            public string? FileName { get; init; }

            /// <summary>
            /// A logical name on the attachment.
            /// </summary>
            [JsonPropertyName("name")]
            public required string Name { get; init; }

            /// <summary>
            /// The name of the Restriction Policy restricting access to this element
            /// </summary>
            /// <remarks>
            /// An empty value indicates no restriction above the ones governing the correspondence referencing this attachment
            /// </remarks>
            [JsonPropertyName("restrictionName")]
            public string? RestrictionName { get; init; }

            /// <summary>
            /// A value indicating whether the attachment is encrypted or not.
            /// </summary>
            [JsonPropertyName("isEncrypted")]
            public bool IsEncrypted { get; init; }

            /// <summary>
            /// MD5 checksum for file data.
            /// </summary>
            [JsonPropertyName("checksum")]
            public string? Checksum { get; init; }

            /// <summary>
            /// A reference value given to the attachment by the creator.
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
        ///
        /// </summary>
        public class ExternalReference
        {
            /// <summary>
            /// The Reference Value
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
            /// Specifies that the attachment data is stored in the Altinn Correspondence Storage
            /// </summary>
            AltinnCorrespondenceAttachment,

            /// <summary>
            /// Specifies that the attachment data is stored in an external storage controlled by the sender
            /// </summary>
            ExternalStorage
        }

        /// <summary>
        /// Represents the important statuses for an attachment
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
