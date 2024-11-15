using System.Text.Json.Serialization;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

partial class CorrespondenceResponse
{
    /// <summary>
    /// Response after a successful <see cref="CorrespondenceClient.Send"/> request
    /// </summary>
    public sealed record Send
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

        /// <summary>
        /// Details about the correspondence
        /// </summary>
        public sealed record CorrespondenceDetails
        {
            /// <summary>
            /// The correspondence identifier
            /// </summary>
            [JsonPropertyName("correspondenceId")]
            public Guid CorrespondenceId { get; init; }

            /// <summary>
            /// The status of the correspondence
            /// </summary>
            [JsonPropertyName("status")]
            public CorrespondenceStatus Status { get; init; }

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
            public List<SendNotificationDetails>? Notifications { get; init; }
        }

        /// <summary>
        /// Details about the correspondence notification
        /// </summary>
        public sealed record SendNotificationDetails
        {
            /// <summary>
            /// The notification order identifier
            /// </summary>
            [JsonPropertyName("orderId")]
            public Guid? OrderId { get; init; }

            /// <summary>
            /// Whether or not this is a reminder notification
            /// </summary>
            [JsonPropertyName("isReminder")]
            public bool? IsReminder { get; init; }

            /// <summary>
            /// The status of the notification
            /// </summary>
            [JsonPropertyName("status")]
            public NotificationStatus Status { get; init; }
        }
    }
}
