using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

public sealed record FiksArkivSettings
{
    [JsonPropertyName("errorNotificationEmailAddress")]
    public string? ErrorNotificationEmailAddress { get; set; }

    [JsonPropertyName("autoSend")]
    [ValidateObjectMembers]
    public FiksArkivAutoSendSettings? AutoSend { get; set; }
}

public sealed record FiksArkivAutoSendSettings
{
    [JsonPropertyName("afterTaskId")]
    public required string AfterTaskId { get; set; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    [JsonPropertyName("attachments")]
    public IReadOnlyList<FiksArkivAttachmentSettings>? Attachments { get; set; }
}

public sealed record FiksArkivAttachmentSettings
{
    [JsonPropertyName("dataType")]
    public required string DataType { get; set; }

    [JsonPropertyName("filename")]
    public required string Filename { get; set; }
}
