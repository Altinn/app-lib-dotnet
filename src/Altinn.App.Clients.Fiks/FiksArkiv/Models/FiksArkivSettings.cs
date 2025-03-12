using System.Text.Json.Serialization;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

public sealed record FiksArkivSettings
{
    [JsonPropertyName("errorNotificationEmailAddress")]
    public string? ErrorNotificationEmailAddress { get; init; }

    [JsonPropertyName("autoSend")]
    public FiksArkivAutoSendSettings? AutoSend { get; init; }
}

public sealed record FiksArkivAutoSendSettings
{
    [JsonPropertyName("afterTaskId")]
    public required string AfterTaskId { get; init; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    [JsonPropertyName("formDocument")]
    public FiksArkivPayloadSettings? FormDocument { get; init; }

    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivPayloadSettings>? Attachments { get; init; }
}

public sealed record FiksArkivPayloadSettings
{
    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    [JsonPropertyName("filename")]
    public string? Filename { get; init; }
}
