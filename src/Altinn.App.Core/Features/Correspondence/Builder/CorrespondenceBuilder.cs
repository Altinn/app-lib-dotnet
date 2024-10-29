using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Base functionality for correspondence builders
/// </summary>
public abstract class CorrespondenceBuilderBase
{
    /// <summary>
    /// Because of the interface-chaining in this builder, some properties are guaranteed to be non-null.
    /// But the compiler doesn't trust that, so we add this check where needed.
    /// </summary>
    /// <param name="value">The value to assert</param>
    /// <param name="errorMessage">The error message to throw, if the value was null</param>
    /// <exception cref="CorrespondenceValueException"></exception>
    internal static void NotNull([NotNull] object? value, string errorMessage)
    {
        if (value is null)
        {
            throw new CorrespondenceValueException(errorMessage);
        }
    }
}

/// <summary>
/// Builder factory for creating <see cref="Models.Correspondence"/> objects
/// </summary>
public class CorrespondenceBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceBuilderResourceId,
        ICorrespondenceBuilderContent,
        ICorrespondenceBuilderSender,
        ICorrespondenceBuilderSendersReference,
        ICorrespondenceBuilderAllowSystemDeleteAfter,
        ICorrespondenceBuilderDueDateTime,
        ICorrespondenceBuilderRecipients,
        ICorrespondenceBuilderBuild
{
    private string? _resourceId;
    private OrganisationNumber? _sender;
    private string? _sendersReference;
    private CorrespondenceContent? _content;
    private DateTimeOffset? _allowSystemDeleteAfter;
    private DateTimeOffset? _dueDateTime;
    private IReadOnlyList<OrganisationNumber>? _recipients;
    private DateTimeOffset? _requestedPublishTime;

    // private string? _messageSender;
    private IReadOnlyList<CorrespondenceExternalReference>? _externalReferences;
    private IReadOnlyDictionary<string, string>? _propertyList;
    private IReadOnlyList<CorrespondenceReplyOptions>? _replyOptions;
    private CorrespondenceNotification? _notification;
    private bool? _ignoreReservation;
    private IReadOnlyList<Guid>? _existingAttachments;

    private CorrespondenceBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceBuilder"/> instance
    /// </summary>
    public static ICorrespondenceBuilderResourceId Create() => new CorrespondenceBuilder();

    /// <inheritdoc/>
    public ICorrespondenceBuilderSender WithResourceId(string resourceId)
    {
        _resourceId = resourceId;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderSendersReference WithSender(OrganisationNumber sender)
    {
        _sender = sender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderRecipients WithSendersReference(string sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderDueDateTime WithRecipients(IReadOnlyList<OrganisationNumber> recipients)
    {
        _recipients = recipients;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderAllowSystemDeleteAfter WithDueDateTime(DateTimeOffset dueDateTime)
    {
        _dueDateTime = dueDateTime;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderContent WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter)
    {
        _allowSystemDeleteAfter = allowSystemDeleteAfter;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithContent(CorrespondenceContent content)
    {
        _content = content;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithContent(ICorrespondenceContentBuilderBuild builder)
    {
        return WithContent(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithRequestedPublishTime(DateTimeOffset requestedPublishTime)
    {
        _requestedPublishTime = requestedPublishTime;
        return this;
    }

    // TODO: This is not fully implemented by Altinn Correspondence yet (Re: Celine @ Team Melding)
    /*
    /// <inheritdoc/>
    public IBuildStep WithMessageSender(string messageSender)
    {
        _messageSender = messageSender;
        return this;
    }
    */

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference> externalReferences
    )
    {
        _externalReferences = externalReferences;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithPropertyList(IReadOnlyDictionary<string, string> propertyList)
    {
        _propertyList = propertyList;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOptions> replyOptions)
    {
        _replyOptions = replyOptions;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithNotification(CorrespondenceNotification notification)
    {
        _notification = notification;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithNotification(ICorrespondenceNotificationBuilderBuild builder)
    {
        return WithNotification(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithIgnoreReservation(bool ignoreReservation)
    {
        _ignoreReservation = ignoreReservation;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithExistingAttachments(IReadOnlyList<Guid> existingAttachments)
    {
        _existingAttachments = existingAttachments;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithAttachment(CorrespondenceAttachment attachment)
    {
        return WithAttachments([attachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithAttachment(ICorrespondenceAttachmentBuilderBuild attachment)
    {
        return WithAttachments([attachment.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments)
    {
        NotNull(_content, "Content is required before adding attachments");

        _content = _content with { Attachments = [.. _content.Attachments ?? [], .. attachments] };
        return this;
    }

    /// <inheritdoc/>
    public Models.Correspondence Build()
    {
        NotNull(_resourceId, "Resource ID is required");
        NotNull(_sender, "Sender is required");
        NotNull(_sendersReference, "Senders reference is required");
        NotNull(_content, "Content is required");
        NotNull(_allowSystemDeleteAfter, "AllowSystemDeleteAfter is required");
        NotNull(_dueDateTime, "DueDateTime is required");
        NotNull(_recipients, "Recipients is required");

        return new Models.Correspondence
        {
            ResourceId = _resourceId,
            Sender = _sender.Value,
            SendersReference = _sendersReference,
            Content = _content,
            AllowSystemDeleteAfter = _allowSystemDeleteAfter.Value,
            DueDateTime = _dueDateTime.Value,
            Recipients = _recipients,
            RequestedPublishTime = _requestedPublishTime,
            // MessageSender = _messageSender,
            ExternalReferences = _externalReferences,
            PropertyList = _propertyList,
            ReplyOptions = _replyOptions,
            Notification = _notification,
            IgnoreReservation = _ignoreReservation,
            ExistingAttachments = _existingAttachments
        };
    }
}
