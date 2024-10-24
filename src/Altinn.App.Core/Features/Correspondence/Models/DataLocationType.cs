namespace Altinn.App.Core.Features.Correspondence.Models;

public enum DataLocationType
{
    /// <summary>
    /// Specifies that the attachment data will need to be uploaded to Altinn Correspondence via the Upload Attachment operation
    /// </summary>
    NewCorrespondenceAttachment = 0,

    /// <summary>
    /// Specifies that the attachment  already exist in Altinn Correpondence storage
    /// </summary>
    ExistingCorrespondenceAttachment = 1,

    /// <summary>
    /// Specifies that the attachment data already exist in an external storage
    /// </summary>
    ExisitingExternalStorage = 2
}
