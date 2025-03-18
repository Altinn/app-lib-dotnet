using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// Represents the settings for the Fiks Arkiv client.
/// </summary>
public sealed record FiksArkivSettings
{
    /// <summary>
    /// The email address to send error notifications to.
    /// </summary>
    [JsonPropertyName("errorNotificationEmailAddress")]
    public string? ErrorNotificationEmailAddress { get; init; }

    /// <summary>
    /// The settings for auto-submissions to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("autoSend")]
    public FiksArkivAutoSendSettings? AutoSend { get; init; }
}

/// <summary>
/// Represents the settings for auto-sending a message to Fiks Arkiv.
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
    /// This is a Fiks IO account ID, which needs to be a GUID.
    /// Either specified directly or a reference to the data model (dot notation).
    /// </summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    /// <summary>
    /// The settings for the form document payload. This is the main data model for the form data,
    /// which will eventually be sent as a "Hoveddokument" to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("formDocument")]
    public FiksArkivPayloadSettings? FormDocument { get; init; }

    /// <summary>
    /// Optional settings for attachments. These are additional documents that will be sent as "Vedlegg" to Fiks Arkiv.
    /// </summary>
    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivPayloadSettings>? Attachments { get; init; }
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
