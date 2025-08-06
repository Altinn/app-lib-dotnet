namespace Altinn.App.Core.Features.Correspondence.Models.Response;

/// <summary>
/// Status of an attachment
/// </summary>
internal sealed record AttachmentOverview
{
    /// <summary>
    /// Unique Id for this attachment
    /// </summary>
    internal required Guid AttachmentId { get; set; }

    /// <summary>
    /// Current attachment status
    /// </summary>
    internal required string Status { get; set; }

    /// <summary>
    /// Current attachment status text description
    /// </summary>
    internal required string StatusText { get; set; }

    /// <summary>
    /// Timestamp for when the Current Attachment Status was changed
    /// </summary>
    internal required DateTimeOffset StatusChanged { get; set; }
}
