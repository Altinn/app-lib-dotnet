using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Signing.Mocks;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class CorrespondenceClientMock
{
    public static async Task<InitializeCorrespondencesResponseMock> Initialize(
        InitializeCorrespondenceRequestMock requestMock
    )
    {
        var responseMock = new InitializeCorrespondencesResponseMock
        {
            CorrespondenceIds = [Guid.NewGuid(), Guid.NewGuid()],
            AttachmentIds = requestMock.ExistingAttachments,
        };

        return await Task.FromResult(responseMock);
    }
}

public class InitializeCorrespondencesResponseMock
{
    public List<Guid>? CorrespondenceIds { get; set; }
    public List<Guid>? AttachmentIds { get; set; }
}

public class InitializeCorrespondenceRequestMock
{
    public required BaseCorrespondenceExt Correspondence { get; set; }
    public required List<string> Recipients { get; set; }
    public List<Guid> ExistingAttachments { get; set; } = [];
}

public class BaseCorrespondenceExt
{
    /// <summary>
    /// Gets or sets the Resource Id for the correspondence service.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [StringLength(255, MinimumLength = 1)]
    [Required]
    public required string ResourceId { get; set; }

    /// <summary>
    /// The Sending organisation of the correspondence.
    /// </summary>
    /// <remarks>
    /// Organization number in countrycode:organizationnumber format.
    /// </remarks>
    [JsonPropertyName("sender")]
    [RegularExpression(
        @"^\d{4}:\d{9}$",
        ErrorMessage = "Organization numbers should be on the form countrycode:organizationnumber, for instance 0192:910753614"
    )]
    [Required]
    public required string Sender { get; set; }

    /// <summary>
    /// Used by senders and receivers to identify specific a Correspondence using external identification methods.
    /// </summary>
    [JsonPropertyName("sendersReference")]
    [StringLength(4096, MinimumLength = 1)]
    [Required]
    public required string SendersReference { get; set; }

    /// <summary>
    /// An alternative name for the sender of the correspondence. The name will be displayed instead of the organization name.
    ///  </summary>
    [JsonPropertyName("messageSender")]
    [StringLength(256, MinimumLength = 0)]
    public string? MessageSender { get; set; }

    /// <summary>
    /// The correspondence content. Contains information about the Correspondence body, subject etc.
    /// </summary>
    [JsonPropertyName("content")]
    public InitializeCorrespondenceContentExt? Content { get; set; }

    /// <summary>
    /// When the correspondence should become visible to the recipient.
    /// </summary>
    [JsonPropertyName("visibleFrom")]
    public required DateTimeOffset VisibleFrom { get; set; }

    /// <summary>
    /// Gets or sets the date for when Altinn can remove the correspondence from its database.
    /// </summary>
    [JsonPropertyName("allowSystemDeleteAfter")]
    public DateTimeOffset? AllowSystemDeleteAfter { get; set; }

    /// <summary>
    /// Gets or sets a date and time for when the recipient must reply.
    /// </summary>
    [JsonPropertyName("dueDateTime")]
    public DateTimeOffset DueDateTime { get; set; }

    /// <summary>
    /// Gets or sets an list of references Senders can use this field to tell the recipient that the correspondence is related to the referenced item(s)
    /// Examples include Altinn App instances, Altinn Broker File Transfers
    /// </summary>
    /// <remarks>
    /// </remarks>
    [JsonPropertyName("externalReferences")]
    public List<ExternalReferenceExt>? ExternalReferences { get; set; }

    /// <summary>
    /// User-defined properties related to the Correspondence
    /// </summary>
    [JsonPropertyName("propertyList")]
    public Dictionary<string, string> PropertyList { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Options for how the recipient can reply to the correspondence
    /// </summary>
    [JsonPropertyName("replyOptions")]
    public List<CorrespondenceReplyOptionExt> ReplyOptions { get; set; } = new List<CorrespondenceReplyOptionExt>();

    /// <summary>
    /// Notifications directly related to this Correspondence.
    /// </summary>
    [JsonPropertyName("notification")]
    public InitializeCorrespondenceNotificationExt? Notification { get; set; }

    /// <summary>
    /// Specifies whether the correspondence can override reservation against digital comminication in KRR
    /// </summary>
    [JsonPropertyName("isReservable")]
    public bool? IsReservable { get; set; }
}

public class ExternalReferenceExt
{
    /// <summary>
    /// The Reference Value
    /// </summary>
    [JsonPropertyName("referenceValue")]
    public required string ReferenceValue { get; set; }

    /// <summary>
    /// The Type of reference
    /// </summary>
    [JsonPropertyName("referenceType")]
    public required ReferenceTypeExt ReferenceType { get; set; }
}

public enum ReferenceTypeExt : int
{
    /// <summary>
    /// Specifies a generic reference
    /// </summary>
    Generic = 0,

    /// <summary>
    /// Specifies that the reference is to a Altinn App Instance
    /// </summary>
    AltinnAppInstance = 1,

    /// <summary>
    /// Specifies that the reference is to a Altinn Broker File Transfer
    /// </summary>
    AltinnBrokerFileTransfer = 2,

    /// <summary>
    /// Specifies that the reference is a Dialogporten Dialog ID
    /// </summary>
    DialogportenDialogId = 3,

    /// <summary>
    /// Specifies that the reference is a Dialogporten Process ID
    /// </summary>
    DialogportenProcessId = 4,
}

public class CorrespondenceReplyOptionExt
{
    /// <summary>
    /// Gets or sets the URL to be used as a reply/response to a correspondence.
    /// </summary>
    public required string LinkURL { get; set; }

    /// <summary>
    /// Gets or sets the url text.
    /// </summary>
    public string? LinkText { get; set; }
}

public class InitializeCorrespondenceNotificationExt
{
    /// <summary>
    /// Which of the notifcation templates to use for this notification
    /// </summary>
    /// <remarks>
    /// Assumed valid variants:
    /// Email, SMS, EmailReminder, SMSReminder
    /// Reminders sent after 14 days if Correspondence not confirmed
    /// </remarks>
    [JsonPropertyName("notificationTemplate")]
    public required NotificationTemplateExt NotificationTemplate { get; set; }

    /// <summary>
    /// The email template to use for this notification
    /// </summary>
    [JsonPropertyName("emailSubject")]
    [StringLength(128, MinimumLength = 0)]
    public string? EmailSubject { get; set; }

    /// <summary>
    /// The email template to use for this notification
    /// </summary>
    [JsonPropertyName("emailBody")]
    [StringLength(1024, MinimumLength = 0)]
    public string? EmailBody { get; set; }

    /// <summary>
    /// The sms template to use for this notification
    /// </summary>
    [JsonPropertyName("smsBody")]
    [StringLength(160, MinimumLength = 0)]
    public string? SmsBody { get; set; }

    /// <summary>
    /// Should a reminder be sent if the notification is not confirmed
    /// </summary>
    [JsonPropertyName("sendReminder")]
    public bool SendReminder { get; set; }

    /// <summary>
    /// The email template to use for this notification
    /// </summary>
    [JsonPropertyName("reminderEmailSubject")]
    [StringLength(128, MinimumLength = 0)]
    public string? ReminderEmailSubject { get; set; }

    /// <summary>
    /// The email template to use for this notification
    /// </summary>
    [JsonPropertyName("reminderEmailBody")]
    [StringLength(1024, MinimumLength = 0)]
    public string? ReminderEmailBody { get; set; }

    /// <summary>
    /// The sms template to use for this notification
    /// </summary>
    [JsonPropertyName("reminderSmsBody")]
    [StringLength(160, MinimumLength = 0)]
    public string? ReminderSmsBody { get; set; }

    /// <summary>
    /// Where to send the notification
    /// </summary>
    public NotificationChannelExt NotificationChannel { get; set; }

    /// <summary>
    /// Where to send the reminder notification
    /// </summary>
    public NotificationChannelExt? ReminderNotificationChannel { get; set; }

    /// <summary>
    /// Senders Reference for this notification
    /// </summary>
    [JsonPropertyName("sendersReference")]
    public string? SendersReference { get; set; }

    /// <summary>
    /// The date and time for when the notification should be sent.
    /// </summary>
    [JsonPropertyName("requestedSendTime")]
    public DateTimeOffset? RequestedSendTime { get; set; }
}

public enum NotificationTemplateExt
{
    /// <summary>
    /// Fully customizable template.
    /// </summary>
    CustomMessage,

    /// <summary>
    /// Standard Altinn notification template.
    /// </summary>
    GenericAltinnMessage,
}

public enum NotificationChannelExt
{
    /// <summary>
    /// The selected channel for the notification is email.
    /// </summary>
    Email,

    /// <summary>
    /// The selected channel for the notification is sms.
    /// </summary>
    Sms,

    /// <summary>
    /// The selected channel for the notification is email preferred.
    /// </summary>
    EmailPreferred,

    /// <summary>
    /// The selected channel for the notification is SMS preferred.
    /// </summary>
    SmsPreferred,
}

public class InitializeCorrespondenceContentExt
{
    /// <summary>
    /// Gets or sets the language of the correspondence, specified according to ISO 639-1
    /// </summary>
    [JsonPropertyName("language")]
    public required string Language { get; set; }

    /// <summary>
    /// Gets or sets the correspondence message title. Subject.
    /// </summary>
    /// <remarks>
    /// TODO: Length restriction?
    /// </remarks>
    [JsonPropertyName("messageTitle")]
    public required string MessageTitle { get; set; }

    /// <summary>
    /// Gets or sets a summary text of the correspondence.
    /// </summary>
    /// <remarks>
    /// TODO: Length restriction?
    /// </remarks>
    [JsonPropertyName("messageSummary")]
    public required string MessageSummary { get; set; }

    /// <summary>
    /// Gets or sets the main body of the correspondence.
    /// </summary>
    public required string MessageBody { get; set; }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
