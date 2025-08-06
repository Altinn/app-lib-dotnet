namespace Altinn.App.Core.Features.Correspondence.Models.Response;

public class AttachmentOverview
{
    /// <summary>
    /// Unique Id for this attachment
    /// </summary>
    public Guid AttachmentId { get; set; }

    /// <summary>
    /// Current attachment status
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Current attachment status text description
    /// </summary>
    public string StatusText { get; set; }

    /// <summary>
    /// Timestamp for when the Current Attachment Status was changed
    /// </summary>
    public DateTimeOffset StatusChanged { get; set; }

    /// <summary>
    /// List of correspondences that are using this attachment
    /// </summary>
    public List<Guid> CorrespondenceIds { get; set; }

    /// <summary>
    /// The attachment data type in MIME format
    /// </summary>
    public string DataType { get; set; }
}
