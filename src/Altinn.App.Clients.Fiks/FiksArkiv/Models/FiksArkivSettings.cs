using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// Represents the Fiks Arkiv settings.
/// </summary>
public sealed record FiksArkivSettings
{
    /// <summary>
    /// Settings related to error handling.
    /// </summary>
    [JsonPropertyName("errorHandling")]
    public FiksArkivErrorHandlingSettings? ErrorHandling { get; set; }

    /// <summary>
    /// Settings related to auto-submission to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("autoSend")]
    public FiksArkivAutoSendSettings? AutoSend { get; set; }
}

/// <summary>
/// Represents the settings for error handling.
/// </summary>
public sealed record FiksArkivErrorHandlingSettings
{
    /// <summary>
    /// Should we send email notifications?
    /// </summary>
    [JsonPropertyName("sendEmailNotifications")]
    public bool? SendEmailNotifications { get; init; }

    /// <summary>
    /// The email addresses to send error notifications to.
    /// </summary>
    [JsonPropertyName("emailNotificationRecipients")]
    public IEnumerable<string>? EmailNotificationRecipients { get; init; }
}

/// <summary>
/// Represents the settings for auto-sending a message.
/// </summary>
public sealed record FiksArkivAutoSendSettings
{
    /// <summary>
    /// The task ID to send the message after.
    /// </summary>
    [JsonPropertyName("afterTaskId")]
    public required string AfterTaskId { get; init; }

    /// <summary>
    /// The recipient of the message.
    /// </summary>
    [JsonPropertyName("recipient")]
    public FiksArkivRecipientSettings? Recipient { get; init; }

    /// <summary>
    /// The settings for the primary document payload.
    /// This is usually the main data model for the form data, or the PDF representation of this data,
    /// which will eventually be sent as a `Hoveddokument` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("primaryDocument")]
    public FiksArkivPayloadSettings? PrimaryDocument { get; init; }

    /// <summary>
    /// Optional settings for attachments. These are additional documents that will be sent as `Vedlegg` to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivPayloadSettings>? Attachments { get; init; }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv recipient.
/// </summary>
public sealed record FiksArkivRecipientSettings
{
    /// <summary>
    /// The account ID of the recipient.
    /// </summary>
    [JsonPropertyName("accountId")]
    public Guid? AccountId { get; init; }

    /// <summary>
    /// A data model binding for the account ID of the recipient.
    /// </summary>
    [JsonPropertyName("dataModelBinding")]
    public FiksArkivDataModelBindingSettings? DataModelBinding { get; init; }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv recipient data model binding.
/// </summary>
public sealed record FiksArkivDataModelBindingSettings
{
    /// <summary>
    /// The data type of the binding (e.g. `Model`)
    /// </summary>
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    /// <summary>
    /// The field that contains the account ID. Dot notation is supported.
    /// </summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }
}

/// <summary>
/// Represents the settings for a Fiks Arkiv document or attachment payload.
/// </summary>
public sealed record FiksArkivPayloadSettings
{
    /// <summary>
    /// The data type of the payload.
    /// </summary>
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    /// <summary>
    /// Optional filename for the payload. If not specified, the filename from <see cref="DataElement"/> will be used.
    /// If that also is missing, the filename will be derived from the data type.
    /// </summary>
    [JsonPropertyName("filename")]
    public string? Filename { get; init; }
}
