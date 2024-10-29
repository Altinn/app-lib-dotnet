using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.ResourceId"/> step
/// </summary>
public interface ICorrespondenceBuilderResourceId
{
    /// <summary>
    /// Sets the Resource Id for the correspondence
    /// </summary>
    /// <param name="resourceId">The resource ID as registered in the Altinn Resource Registry</param>
    ICorrespondenceBuilderSender WithResourceId(string resourceId);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.Sender"/> step
/// </summary>
public interface ICorrespondenceBuilderSender
{
    /// <summary>
    /// Sets the sender of the correspondence
    /// </summary>
    /// <param name="sender">The correspondence sender</param>
    ICorrespondenceBuilderSendersReference WithSender(OrganisationNumber sender);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.SendersReference"/> step
/// </summary>
public interface ICorrespondenceBuilderSendersReference
{
    /// <summary>
    /// Sets the senders reference for the correspondence
    /// </summary>
    /// <param name="sendersReference">The correspondence reference</param>
    ICorrespondenceBuilderRecipients WithSendersReference(string sendersReference);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.Recipients"/> step
/// </summary>
public interface ICorrespondenceBuilderRecipients
{
    /// <summary>
    /// Sets the recipients of the correspondence
    /// </summary>
    /// <param name="recipients">A list of recipients</param>
    ICorrespondenceBuilderDueDateTime WithRecipients(IReadOnlyList<OrganisationNumber> recipients);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.DueDateTime"/> step
/// </summary>
public interface ICorrespondenceBuilderDueDateTime
{
    /// <summary>
    /// Sets due date and time for the correspondence
    /// </summary>
    /// <param name="dueDateTime">The point in time when the correspondence is due</param>
    /// <returns></returns>
    ICorrespondenceBuilderAllowSystemDeleteAfter WithDueDateTime(DateTimeOffset dueDateTime);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.AllowSystemDeleteAfter"/> step
/// </summary>
public interface ICorrespondenceBuilderAllowSystemDeleteAfter
{
    /// <summary>
    /// Sets the date and time when the correspondence can be deleted from the system
    /// </summary>
    /// <param name="allowSystemDeleteAfter">The point in time when the correspondence may be safely deleted</param>
    ICorrespondenceBuilderContent WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance is on the <see cref="Models.Correspondence.Content"/> step
/// </summary>
public interface ICorrespondenceBuilderContent
{
    /// <summary>
    /// Sets the content of the correspondence
    /// </summary>
    /// <param name="content">The correspondence content</param>
    ICorrespondenceBuilderBuild WithContent(CorrespondenceContent content);

    /// <summary>
    /// Sets the content of the correspondence
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceContentBuilder"/> instance in the <see cref="ICorrespondenceContentBuilderBuild"/> stage</param>
    ICorrespondenceBuilderBuild WithContent(ICorrespondenceContentBuilderBuild builder);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceBuilder.Build"/>
/// </summary>
public interface ICorrespondenceBuilderBuild
{
    /// <summary>
    /// Sets the requested publish time for the correspondence
    /// </summary>
    /// <param name="requestedPublishTime">The point in time when the correspondence should be published</param>
    ICorrespondenceBuilderBuild WithRequestedPublishTime(DateTimeOffset requestedPublishTime);

    // TODO: This is not fully implemented by Altinn Correspondence yet (Re: Celine @ Team Melding)
    /*
    /// <summary>
    /// Set the message sender for the correspondence
    /// </summary>
    /// <param name="messageSender">The name of the message sender</param>
    /// <returns></returns>
    ICorrespondenceBuilderBuild WithMessageSender(string messageSender);
    */

    /// <summary>
    /// Sets the external references for the correspondence
    /// </summary>
    /// <param name="externalReferences">A list of reference to other items in the Altinn ecosystem</param>
    ICorrespondenceBuilderBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference> externalReferences
    );

    /// <summary>
    /// Sets the property list for the correspondence
    /// </summary>
    /// <param name="propertyList">A key-value list of arbitrary properties to associate with the correspondence</param>
    ICorrespondenceBuilderBuild WithPropertyList(IReadOnlyDictionary<string, string> propertyList);

    /// <summary>
    /// Sets the reply options for the correspondence
    /// </summary>
    /// <param name="replyOptions">A list of options for how the recipient can reply to the correspondence</param>
    ICorrespondenceBuilderBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOptions> replyOptions);

    /// <summary>
    /// Sets the notification for the correspondence
    /// </summary>
    /// <param name="notification">The notification details to be associated with the correspondence</param>
    ICorrespondenceBuilderBuild WithNotification(CorrespondenceNotification notification);

    /// <summary>
    /// Sets the notification for the correspondence
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceNotificationBuilder"/> instance in the <see cref="ICorrespondenceNotificationBuilderBuild"/> stage</param>
    ICorrespondenceBuilderBuild WithNotification(ICorrespondenceNotificationBuilderBuild builder);

    /// <summary>
    /// Sets whether the correspondence can override reservation against digital communication in KRR
    /// </summary>
    /// <param name="ignoreReservation">A boolean value indicating whether or not reservations can be ignored</param>
    ICorrespondenceBuilderBuild WithIgnoreReservation(bool ignoreReservation);

    /// <summary>
    /// Sets the existing attachments that should be added to the correspondence
    /// </summary>
    /// <param name="existingAttachments">A list of <see cref="Guid"/>s pointing to existing attachments</param>
    ICorrespondenceBuilderBuild WithExistingAttachments(IReadOnlyList<Guid> existingAttachments);

    /// <summary>
    /// Adds an attachment to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="attachment">A <see cref="CorrespondenceAttachment"/> item</param>
    ICorrespondenceBuilderBuild WithAttachment(CorrespondenceAttachment attachment);

    /// <summary>
    /// Adds an attachment to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceAttachmentBuilder"/> instance in the <see cref="ICorrespondenceAttachmentBuilderBuild"/> stage</param>
    ICorrespondenceBuilderBuild WithAttachment(ICorrespondenceAttachmentBuilderBuild builder);

    /// <summary>
    /// Adds attachments to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="attachments">A List of <see cref="CorrespondenceAttachment"/> items</param>
    ICorrespondenceBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments);

    /// <summary>
    /// Builds the <see cref="Models.Correspondence"/> instance
    /// </summary>
    Models.Correspondence Build();
}
