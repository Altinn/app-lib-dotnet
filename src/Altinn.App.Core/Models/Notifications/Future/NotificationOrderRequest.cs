using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Notifications.Future;

/// <summary>
/// Represents a request for creating a notification order in the Altinn Notifications service.
/// </summary>
public sealed record NotificationOrderRequest
{
    /// <summary>
    /// Gets or sets the idempotency identifier for this request.
    /// </summary>
    /// <remarks>
    /// Repeated requests with the same identifier will only result in one notification order being created.
    /// </remarks>
    [JsonPropertyName("idempotencyId")]
    public required string IdempotencyId { get; init; }

    /// <summary>
    /// Gets or sets an optional reference identifier from the app.
    /// </summary>
    /// <remarks>
    /// Use this to correlate the notification order with a record in your app, such as an instance ID.
    /// </remarks>
    [JsonPropertyName("sendersReference")]
    public required string SendersReference { get; init; }

    /// <summary>
    /// Gets or sets the earliest time the notification should be sent.
    /// </summary>
    /// <remarks>
    /// Defaults to the current UTC time, meaning the notification will be sent as soon as possible.
    /// </remarks>
    [JsonPropertyName("requestedSendTime")]
    public DateTime RequestedSendTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets an optional endpoint the Altinn Notifications service will call to determine
    /// whether the notification should still be sent at delivery time.
    /// </summary>
    /// <remarks>
    /// Useful for notifications scheduled in the future where the condition for sending may no longer
    /// apply by the time the send time is reached.
    /// </remarks>
    [JsonPropertyName("conditionEndpoint")]
    public Uri? ConditionEndpoint { get; init; }

    /// <summary>
    /// Gets or sets the recipient of the notification.
    /// </summary>
    [JsonPropertyName("recipient")]
    public required NotificationRecipient Recipient { get; init; }
}

/// <summary>
/// Defines the recipient of a notification order. Exactly one recipient type should be set.
/// </summary>
public sealed record NotificationRecipient
{
    /// <summary>
    /// Gets or sets a recipient identified by a direct email address.
    /// </summary>
    [JsonPropertyName("recipientEmail")]
    public RecipientEmail? RecipientEmail { get; init; }

    /// <summary>
    /// Gets or sets a recipient identified by a direct phone number.
    /// </summary>
    [JsonPropertyName("recipientSms")]
    public RecipientSms? RecipientSms { get; init; }

    /// <summary>
    /// Gets or sets a recipient identified by a Norwegian national identity number.
    /// </summary>
    /// <remarks>
    /// Contact information will be retrieved from the Common Contact Register (KRR).
    /// </remarks>
    [JsonPropertyName("recipientPerson")]
    public RecipientPerson? RecipientPerson { get; init; }

    /// <summary>
    /// Gets or sets a recipient identified by a Norwegian organization number.
    /// </summary>
    /// <remarks>
    /// Contact information will be retrieved from the Central Coordinating Register for Legal Entities (Enhetsregisteret).
    /// </remarks>
    [JsonPropertyName("recipientOrganization")]
    public RecipientOrganization? RecipientOrganization { get; init; }

    /// <summary>
    /// Gets or sets a recipient identified by an external identity, used for self-identified users
    /// who authenticate via ID-porten email login.
    /// </summary>
    /// <remarks>
    /// Contact information will be retrieved from Altinn Profile using the external identity.
    /// </remarks>
    [JsonPropertyName("recipientSelfIdentifiedUser")]
    public RecipientSelfIdentifiedUser? RecipientSelfIdentifiedUser { get; init; }
}

/// <summary>
/// Identifies a notification recipient by a direct email address.
/// </summary>
public sealed record RecipientEmail
{
    /// <summary>
    /// Gets or sets the recipient's email address.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; }

    /// <summary>
    /// Gets or sets the email content and delivery options.
    /// </summary>
    [JsonPropertyName("emailSettings")]
    public required EmailSendingOptions Settings { get; init; }
}

/// <summary>
/// Identifies a notification recipient by a direct phone number.
/// </summary>
public sealed record RecipientSms
{
    /// <summary>
    /// Gets or sets the recipient's phone number in international format.
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// Gets or sets the SMS content and delivery options.
    /// </summary>
    [JsonPropertyName("smsSettings")]
    public required SmsSendingOptions Settings { get; init; }
}

/// <summary>
/// Identifies a notification recipient by a Norwegian national identity number.
/// </summary>
public sealed record RecipientPerson
{
    /// <summary>
    /// Gets or sets the recipient's national identity number.
    /// </summary>
    [JsonPropertyName("nationalIdentityNumber")]
    public required string NationalIdentityNumber { get; init; }

    /// <summary>
    /// Gets or sets an optional Altinn resource identifier used for authorization and auditing.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets or sets the notification channel to use.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="NotificationChannel.EmailPreferred"/>, meaning email will be attempted
    /// first with SMS as fallback.
    /// </remarks>
    [JsonPropertyName("channelSchema")]
    public NotificationChannel ChannelSchema { get; init; } = NotificationChannel.EmailPreferred;

    /// <summary>
    /// Gets or sets whether to ignore the recipient's reservation against electronic communication in KRR.
    /// </summary>
    [JsonPropertyName("ignoreReservation")]
    public bool IgnoreReservation { get; init; } = false;

    /// <summary>
    /// Gets or sets email content and delivery options. Required when the channel scheme includes email.
    /// </summary>
    [JsonPropertyName("emailSettings")]
    public EmailSendingOptions? EmailSettings { get; init; }

    /// <summary>
    /// Gets or sets SMS content and delivery options. Required when the channel scheme includes SMS.
    /// </summary>
    [JsonPropertyName("smsSettings")]
    public SmsSendingOptions? SmsSettings { get; init; }
}

/// <summary>
/// Identifies a notification recipient by a Norwegian organization number.
/// </summary>
public sealed record RecipientOrganization
{
    /// <summary>
    /// Gets or sets the organization number.
    /// </summary>
    [JsonPropertyName("orgNumber")]
    public required string OrgNumber { get; init; }

    /// <summary>
    /// Gets or sets an optional Altinn resource identifier used for authorization and auditing.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets or sets the notification channel to use.
    /// </summary>
    [JsonPropertyName("channelSchema")]
    public required NotificationChannel ChannelSchema { get; init; }

    /// <summary>
    /// Gets or sets email content and delivery options. Required when the channel scheme includes email.
    /// </summary>
    [JsonPropertyName("emailSettings")]
    public EmailSendingOptions? EmailSettings { get; init; }

    /// <summary>
    /// Gets or sets SMS content and delivery options. Required when the channel scheme includes SMS.
    /// </summary>
    [JsonPropertyName("smsSettings")]
    public SmsSendingOptions? SmsSettings { get; init; }
}

/// <summary>
/// Identifies a notification recipient by an external identity, used for self-identified users
/// who authenticate via ID-porten email login.
/// </summary>
public sealed record RecipientSelfIdentifiedUser
{
    /// <summary>
    /// Gets or sets the recipient's external identity in URN format.
    /// </summary>
    /// <remarks>
    /// Supported formats:
    /// <list type="bullet">
    /// <item><description><c>urn:altinn:person:idporten-email:{email}</c> — ID-porten email login</description></item>
    /// <item><description><c>urn:altinn:person:legacy-selfidentified:{username}</c> — legacy username/password login</description></item>
    /// </list>
    /// </remarks>
    [JsonPropertyName("externalIdentity")]
    public required string ExternalIdentity { get; init; }

    /// <summary>
    /// Gets or sets an optional Altinn resource identifier used for authorization and auditing.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets or sets the notification channel to use. Defaults to <see cref="NotificationChannel.Email"/>.
    /// </summary>
    [JsonPropertyName("channelSchema")]
    public NotificationChannel ChannelSchema { get; init; } = NotificationChannel.Email;

    /// <summary>
    /// Gets or sets email content and delivery options. Required when the channel scheme includes email.
    /// </summary>
    [JsonPropertyName("emailSettings")]
    public EmailSendingOptions? EmailSettings { get; init; }

    /// <summary>
    /// Gets or sets SMS content and delivery options. Required when the channel scheme includes SMS.
    /// </summary>
    [JsonPropertyName("smsSettings")]
    public SmsSendingOptions? SmsSettings { get; init; }
}

/// <summary>
/// Defines content and delivery options for an email notification.
/// </summary>
public sealed record EmailSendingOptions
{
    /// <summary>
    /// Gets or sets an optional sender email address to display to the recipient.
    /// </summary>
    [JsonPropertyName("senderEmailAddress")]
    public string? SenderEmailAddress { get; init; }

    /// <summary>
    /// Gets or sets the subject line of the email.
    /// </summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>
    /// Gets or sets the body content of the email.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }

    /// <summary>
    /// Gets or sets the content type of the email body. Defaults to <see cref="EmailContentType.Plain"/>.
    /// </summary>
    [JsonPropertyName("contentType")]
    public EmailContentType ContentType { get; init; } = EmailContentType.Plain;

    /// <summary>
    /// Gets or sets when the email may be delivered. Defaults to <see cref="SendingTimePolicy.Anytime"/>.
    /// </summary>
    [JsonPropertyName("sendingTimePolicy")]
    public SendingTimePolicy SendingTimePolicy { get; init; } = SendingTimePolicy.Anytime;
}

/// <summary>
/// Defines content and delivery options for an SMS notification.
/// </summary>
public sealed record SmsSendingOptions
{
    /// <summary>
    /// Gets or sets an optional sender name or number displayed to the recipient.
    /// </summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>
    /// Gets or sets the text content of the SMS.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }

    /// <summary>
    /// Gets or sets when the SMS may be delivered. Defaults to <see cref="SendingTimePolicy.Daytime"/>
    /// to avoid sending messages at unsociable hours.
    /// </summary>
    [JsonPropertyName("sendingTimePolicy")]
    public SendingTimePolicy SendingTimePolicy { get; init; } = SendingTimePolicy.Daytime;
}

/// <summary>
/// Defines the notification channel or channel priority scheme to use when delivering a notification.
/// </summary>
public enum NotificationChannel
{
    /// <summary>Email only.</summary>
    [JsonStringEnumMemberName("Email")]
    Email,

    /// <summary>SMS only.</summary>
    [JsonStringEnumMemberName("Sms")]
    Sms,

    /// <summary>Email first, SMS as fallback if the recipient has no email address.</summary>
    [JsonStringEnumMemberName("EmailPreferred")]
    EmailPreferred,

    /// <summary>SMS first, email as fallback if the recipient has no phone number.</summary>
    [JsonStringEnumMemberName("SmsPreferred")]
    SmsPreferred,

    /// <summary>Both email and SMS are sent simultaneously.</summary>
    [JsonStringEnumMemberName("EmailAndSms")]
    EmailAndSms,
}

/// <summary>
/// Defines the content type of an email body.
/// </summary>
public enum EmailContentType
{
    /// <summary>Plain text.</summary>
    [JsonStringEnumMemberName("Plain")]
    Plain,

    /// <summary>HTML markup.</summary>
    [JsonStringEnumMemberName("Html")]
    Html,
}

/// <summary>
/// Defines when a notification may be delivered.
/// </summary>
public enum SendingTimePolicy
{
    /// <summary>The notification may be sent at any time of day.</summary>
    [JsonStringEnumMemberName("Anytime")]
    Anytime,

    /// <summary>The notification will only be sent during daytime hours.</summary>
    [JsonStringEnumMemberName("Daytime")]
    Daytime,
}
