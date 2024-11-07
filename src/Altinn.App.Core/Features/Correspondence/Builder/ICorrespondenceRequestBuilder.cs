using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.ResourceId"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsResourceId
{
    /// <summary>
    /// Sets the Resource Id for the correspondence
    /// </summary>
    /// <param name="resourceId">The resource ID as registered in the Altinn Resource Registry</param>
    ICorrespondenceBuilderNeedsSender WithResourceId(string resourceId);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.Sender"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsSender
{
    /// <summary>
    /// Sets the sender of the correspondence
    /// </summary>
    /// <param name="sender">The correspondence sender</param>
    ICorrespondenceBuilderNeedsSendersReference WithSender(OrganisationNumber sender);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.SendersReference"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsSendersReference
{
    /// <summary>
    /// Sets the senders reference for the correspondence
    /// </summary>
    /// <param name="sendersReference">The correspondence reference</param>
    ICorrespondenceBuilderNeedsRecipients WithSendersReference(string sendersReference);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.Recipients"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsRecipients
{
    /// <summary>
    /// Sets the recipient of the correspondence
    /// </summary>
    /// <param name="recipient">A recipient</param>
    ICorrespondenceBuilderNeedsDueDateTime WithRecipient(OrganisationNumber recipient);

    /// <summary>
    /// Sets the recipients of the correspondence
    /// </summary>
    /// <param name="recipients">A list of recipients</param>
    ICorrespondenceBuilderNeedsDueDateTime WithRecipients(IReadOnlyList<OrganisationNumber> recipients);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.DueDateTime"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsDueDateTime
{
    /// <summary>
    /// Sets due date and time for the correspondence
    /// </summary>
    /// <param name="dueDateTime">The point in time when the correspondence is due</param>
    /// <returns></returns>
    ICorrespondenceBuilderNeedsAllowSystemDeleteAfter WithDueDateTime(DateTimeOffset dueDateTime);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.AllowSystemDeleteAfter"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsAllowSystemDeleteAfter
{
    /// <summary>
    /// Sets the date and time when the correspondence can be deleted from the system
    /// </summary>
    /// <param name="allowSystemDeleteAfter">The point in time when the correspondence may be safely deleted</param>
    ICorrespondenceBuilderNeedsContent WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance is on the <see cref="CorrespondenceRequest.Content"/> step
/// </summary>
public interface ICorrespondenceBuilderNeedsContent
{
    /// <summary>
    /// Sets the content of the correspondence
    /// </summary>
    /// <param name="content">The correspondence content</param>
    ICorrespondenceBuilderCanBuild WithContent(CorrespondenceContent content);

    /// <summary>
    /// Sets the content of the correspondence
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceContentBuilder"/> instance in the <see cref="ICorrespondenceContentBuilderCanBuild"/> stage</param>
    ICorrespondenceBuilderCanBuild WithContent(ICorrespondenceContentBuilderCanBuild builder);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceRequestBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceRequestBuilder.Build"/>
/// </summary>
public interface ICorrespondenceBuilderCanBuild
{
    /// <summary>
    /// Sets the requested publish time for the correspondence
    /// </summary>
    /// <param name="requestedPublishTime">The point in time when the correspondence should be published</param>
    ICorrespondenceBuilderCanBuild WithRequestedPublishTime(DateTimeOffset requestedPublishTime);

    /// <summary>
    /// Set the message sender for the correspondence
    /// </summary>
    /// <param name="messageSender">The name of the message sender</param>
    /// <returns></returns>
    ICorrespondenceBuilderCanBuild WithMessageSender(string messageSender);

    /// <summary>
    /// Adds an external reference to the correspondence
    /// <remarks>
    /// This method respects any existing references already stored in <see cref="CorrespondenceRequest.ExternalReferences"/>
    /// </remarks>
    /// </summary>
    /// <param name="externalReference">A <see cref="CorrespondenceExternalReference"/> item</param>
    ICorrespondenceBuilderCanBuild WithExternalReference(CorrespondenceExternalReference externalReference);

    /// <summary>
    /// Adds an external reference to the correspondence
    /// <remarks>
    /// This method respects any existing references already stored in <see cref="CorrespondenceRequest.ExternalReferences"/>
    /// </remarks>
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceExternalReferenceBuilder"/> instance in the <see cref="ICorrespondenceExternalReferenceBuilderCanBuild"/> stage</param>
    ICorrespondenceBuilderCanBuild WithExternalReference(ICorrespondenceExternalReferenceBuilderCanBuild builder);

    /// <summary>
    /// Adds external references to the correspondence
    /// <remarks>
    /// This method respects any existing references already stored in <see cref="CorrespondenceRequest.ExternalReferences"/>
    /// </remarks>
    /// </summary>
    /// <param name="externalReferences">A list of <see cref="CorrespondenceExternalReference"/> items</param>
    ICorrespondenceBuilderCanBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference> externalReferences
    );

    /// <summary>
    /// Sets the property list for the correspondence
    /// </summary>
    /// <param name="propertyList">A key-value list of arbitrary properties to associate with the correspondence</param>
    ICorrespondenceBuilderCanBuild WithPropertyList(IReadOnlyDictionary<string, string> propertyList);

    /// <summary>
    /// Adds a reply option to the correspondence
    /// <remarks>
    /// This method respects any existing options already stored in <see cref="CorrespondenceRequest.ReplyOptions"/>
    /// </remarks>
    /// </summary>
    /// <param name="replyOption">A <see cref="CorrespondenceReplyOption"/> item</param>
    ICorrespondenceBuilderCanBuild WithReplyOption(CorrespondenceReplyOption replyOption);

    /// <summary>
    /// Adds a reply option to the correspondence
    /// <remarks>
    /// This method respects any existing options already stored in <see cref="CorrespondenceRequest.ReplyOptions"/>
    /// </remarks>
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceReplyOptionBuilder"/> instance in the <see cref="ICorrespondenceReplyOptionsBuilderCanBuild"/> stage</param>
    ICorrespondenceBuilderCanBuild WithReplyOption(ICorrespondenceReplyOptionsBuilderCanBuild builder);

    /// <summary>
    /// Adds reply options to the correspondence
    /// <remarks>
    /// This method respects any existing options already stored in <see cref="CorrespondenceRequest.ReplyOptions"/>
    /// </remarks>
    /// </summary>
    /// <param name="replyOptions">A list of <see cref="CorrespondenceReplyOption"/> items</param>
    ICorrespondenceBuilderCanBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOption> replyOptions);

    /// <summary>
    /// Sets the notification for the correspondence
    /// </summary>
    /// <param name="notification">The notification details to be associated with the correspondence</param>
    ICorrespondenceBuilderCanBuild WithNotification(CorrespondenceNotification notification);

    /// <summary>
    /// Sets the notification for the correspondence
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceNotificationBuilder"/> instance in the <see cref="ICorrespondenceNotificationBuilderCanBuild"/> stage</param>
    ICorrespondenceBuilderCanBuild WithNotification(ICorrespondenceNotificationBuilderCanBuild builder);

    /// <summary>
    /// Sets whether the correspondence can override reservation against digital communication in KRR
    /// </summary>
    /// <param name="ignoreReservation">A boolean value indicating whether or not reservations can be ignored</param>
    ICorrespondenceBuilderCanBuild WithIgnoreReservation(bool ignoreReservation);

    /// <summary>
    /// Adds an existing attachment reference to the correspondence
    /// <remarks>
    /// This method respects any existing references already stored in <see cref="CorrespondenceRequest.ExistingAttachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="existingAttachment">A <see cref="Guid"/> item pointing to an existing attachment</param>
    ICorrespondenceBuilderCanBuild WithExistingAttachment(Guid existingAttachment);

    /// <summary>
    /// Adds existing attachment references to the correspondence
    /// <remarks>
    /// This method respects any existing references already stored in <see cref="CorrespondenceRequest.ExistingAttachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="existingAttachments">A list of <see cref="Guid"/> items pointing to existing attachments</param>
    ICorrespondenceBuilderCanBuild WithExistingAttachments(IReadOnlyList<Guid> existingAttachments);

    /// <summary>
    /// Adds an attachment to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="attachment">A <see cref="CorrespondenceAttachment"/> item</param>
    ICorrespondenceBuilderCanBuild WithAttachment(CorrespondenceAttachment attachment);

    /// <summary>
    /// Adds an attachment to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceAttachmentBuilder"/> instance in the <see cref="ICorrespondenceAttachmentBuilderCanBuild"/> stage</param>
    ICorrespondenceBuilderCanBuild WithAttachment(ICorrespondenceAttachmentBuilderCanBuild builder);

    /// <summary>
    /// Adds attachments to the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="attachments">A List of <see cref="CorrespondenceAttachment"/> items</param>
    ICorrespondenceBuilderCanBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments);

    /// <summary>
    /// Builds the <see cref="CorrespondenceRequest"/> instance
    /// </summary>
    CorrespondenceRequest Build();
}
