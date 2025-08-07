using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Correspondence.Models.Response;

/// <summary>
/// Status of an attachment
/// </summary>
internal class AttachmentOverview
{
    /// <summary>
    /// Unique Id for this attachment
    /// </summary>
    [JsonPropertyName("attachmentId")]
    public required Guid AttachmentId { get; set; }

    /// <summary>
    /// Current attachment status
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; set; }

    /// <summary>
    /// Current attachment status text description
    /// </summary>
    [JsonPropertyName("statusText")]
    public required string StatusText { get; set; }

    /// <summary>
    /// Timestamp for when the Current Attachment Status was changed
    /// </summary>
    [JsonPropertyName("statusChanged")]
    public required DateTimeOffset StatusChanged { get; set; }
}
