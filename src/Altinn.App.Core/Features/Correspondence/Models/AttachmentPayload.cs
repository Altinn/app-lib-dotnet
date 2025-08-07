namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents the payload for sending an attachment.
/// </summary>
internal class AttachmentPayload
{
    /// <summary>
    /// Gets or sets the Resource Id for the correspondence service.
    /// </summary>
    public required string ResourceId { get; set; }

    /// <summary>
    /// The name of the attachment file.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// A logical name for the file, which will be shown in Altinn Inbox.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// A value indicating whether the attachment is encrypted or not.
    /// </summary>
    public required bool IsEncrypted { get; set; }

    /// <summary>
    /// A reference value given to the attachment by the creator.
    /// </summary>
    public required string SendersReference { get; set; }
}
