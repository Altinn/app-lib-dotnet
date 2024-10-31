using System.Net.Mime;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.Filename"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsFilename
{
    /// <summary>
    /// Sets the filename of the attachment
    /// </summary>
    /// <param name="filename">The attachment filename</param>
    ICorrespondenceAttachmentBuilderNeedsName WithFilename(string filename);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.Name"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsName
{
    /// <summary>
    /// Sets the display name of the attachment
    /// </summary>
    /// <param name="name">The display name</param>
    ICorrespondenceAttachmentBuilderNeedsSender WithName(string name);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.Sender"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsSender
{
    /// <summary>
    /// Sets the sending organisation of the attachment
    /// </summary>
    /// <param name="sender">The organisation number of the sender</param>
    ICorrespondenceAttachmentBuilderNeedsSendersReference WithSender(OrganisationNumber sender);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.SendersReference"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsSendersReference
{
    /// <summary>
    /// Sets the senders reference for the attachment
    /// </summary>
    /// <param name="sendersReference">The reference value</param>
    ICorrespondenceAttachmentBuilderNeedsDataType WithSendersReference(string sendersReference);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.DataType"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsDataType
{
    /// <summary>
    /// Sets the data type of the attachment in MIME format
    /// </summary>
    /// <remarks>See <see cref="MediaTypeNames"/></remarks>
    /// <param name="dataType">The MIME type of the attachment</param>
    ICorrespondenceAttachmentBuilderNeedsData WithDataType(string dataType);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance is on the <see cref="CorrespondenceAttachment.Data"/> step
/// </summary>
public interface ICorrespondenceAttachmentBuilderNeedsData
{
    /// <summary>
    /// Sets the file stream of the attachment
    /// </summary>
    /// <param name="data">The file stream</param>
    ICorrespondenceAttachmentBuilderCanBuild WithData(Stream data);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceAttachmentBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceAttachmentBuilder.Build"/>
/// </summary>
public interface ICorrespondenceAttachmentBuilderCanBuild
{
    /// <summary>
    /// Sets the name of the Restriction Policy restricting access to this element
    /// </summary>
    /// <param name="restrictionName">The name of the restriction policy</param>
    ICorrespondenceAttachmentBuilderCanBuild WithRestrictionName(string restrictionName);

    /// <summary>
    /// Sets whether the attachment is encrypted or not
    /// </summary>
    /// <param name="isEncrypted">`true` for encrypted, `false` otherwise</param>
    ICorrespondenceAttachmentBuilderCanBuild WithIsEncrypted(bool isEncrypted);

    /// <summary>
    /// Sets the storage location of the attachment data
    /// </summary>
    /// <remarks>In this context, it is extremely likely that the storage location is <see cref="CorrespondenceDataLocationType.ExistingCorrespondenceAttachment"/></remarks>
    /// <param name="dataLocationType">The data storage location</param>
    ICorrespondenceAttachmentBuilderCanBuild WithDataLocationType(CorrespondenceDataLocationType dataLocationType);

    /// <summary>
    /// Builds the correspondence attachment
    /// </summary>
    CorrespondenceAttachment Build();
}
