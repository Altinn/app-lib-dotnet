namespace Altinn.App.Core.Features.Correspondence.Models.Response;

/// <summary>
/// Status of an attachment
/// </summary>
internal class AttachmentOverview
{
    /// <summary>
    /// Unique Id for this attachment
    /// </summary>
    public required Guid AttachmentId { get; set; }

    /// <summary>
    /// Current attachment status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Current attachment status text description
    /// </summary>
    public required string StatusText { get; set; }

    /// <summary>
    /// Timestamp for when the Current Attachment Status was changed
    /// </summary>
    public required DateTimeOffset StatusChanged { get; set; }
}
