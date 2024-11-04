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
    internal static void NotNullOrEmpty([NotNull] object? value, string errorMessage)
    {
        if (value is null)
        {
            throw new CorrespondenceValueException(errorMessage);
        }

        if (value is string str && string.IsNullOrWhiteSpace(str))
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
        ICorrespondenceBuilderNeedsResourceId,
        ICorrespondenceBuilderNeedsContent,
        ICorrespondenceBuilderNeedsSender,
        ICorrespondenceBuilderNeedsSendersReference,
        ICorrespondenceBuilderNeedsAllowSystemDeleteAfter,
        ICorrespondenceBuilderNeedsDueDateTime,
        ICorrespondenceBuilderNeedsRecipients,
        ICorrespondenceBuilderCanBuild
{
    private string? _resourceId;
    private OrganisationNumber? _sender;
    private string? _sendersReference;
    private CorrespondenceContent? _content;
    private DateTimeOffset? _allowSystemDeleteAfter;
    private DateTimeOffset? _dueDateTime;
    private IReadOnlyList<OrganisationNumber>? _recipients;
    private DateTimeOffset? _requestedPublishTime;
    private string? _messageSender;
    private IReadOnlyList<CorrespondenceExternalReference>? _externalReferences;
    private IReadOnlyDictionary<string, string>? _propertyList;
    private IReadOnlyList<CorrespondenceReplyOption>? _replyOptions;
    private CorrespondenceNotification? _notification;
    private bool? _ignoreReservation;
    private IReadOnlyList<Guid>? _existingAttachments;

    private CorrespondenceBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceBuilder"/> instance
    /// </summary>
    public static ICorrespondenceBuilderNeedsResourceId Create() => new CorrespondenceBuilder();

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsSender WithResourceId(string resourceId)
    {
        _resourceId = resourceId;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsSendersReference WithSender(OrganisationNumber sender)
    {
        _sender = sender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsRecipients WithSendersReference(string sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsDueDateTime WithRecipient(OrganisationNumber recipient)
    {
        return WithRecipients([recipient]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsDueDateTime WithRecipients(IReadOnlyList<OrganisationNumber> recipients)
    {
        _recipients = recipients;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsAllowSystemDeleteAfter WithDueDateTime(DateTimeOffset dueDateTime)
    {
        _dueDateTime = dueDateTime;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderNeedsContent WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter)
    {
        _allowSystemDeleteAfter = allowSystemDeleteAfter;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithContent(CorrespondenceContent content)
    {
        _content = content;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithContent(ICorrespondenceContentBuilderCanBuild builder)
    {
        return WithContent(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithRequestedPublishTime(DateTimeOffset requestedPublishTime)
    {
        _requestedPublishTime = requestedPublishTime;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithMessageSender(string messageSender)
    {
        _messageSender = messageSender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithExternalReference(CorrespondenceExternalReference externalReference)
    {
        return WithExternalReferences([externalReference]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithExternalReference(ICorrespondenceExternalReferenceBuilderCanBuild builder)
    {
        return WithExternalReferences([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference> externalReferences
    )
    {
        _externalReferences = [.. _externalReferences ?? [], .. externalReferences];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithPropertyList(IReadOnlyDictionary<string, string> propertyList)
    {
        _propertyList = propertyList;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithReplyOption(CorrespondenceReplyOption replyOption)
    {
        return WithReplyOptions([replyOption]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithReplyOption(ICorrespondenceReplyOptionsBuilderCanBuild builder)
    {
        return WithReplyOptions([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOption> replyOptions)
    {
        _replyOptions = [.. _replyOptions ?? [], .. replyOptions];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithNotification(CorrespondenceNotification notification)
    {
        _notification = notification;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithNotification(ICorrespondenceNotificationBuilderCanBuild builder)
    {
        return WithNotification(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithIgnoreReservation(bool ignoreReservation)
    {
        _ignoreReservation = ignoreReservation;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithExistingAttachment(Guid existingAttachment)
    {
        return WithExistingAttachments([existingAttachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithExistingAttachments(IReadOnlyList<Guid> existingAttachments)
    {
        _existingAttachments = [.. _existingAttachments ?? [], .. existingAttachments];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithAttachment(CorrespondenceAttachment attachment)
    {
        return WithAttachments([attachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithAttachment(ICorrespondenceAttachmentBuilderCanBuild attachment)
    {
        return WithAttachments([attachment.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderCanBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments)
    {
        NotNullOrEmpty(_content, "Content is required before adding attachments");

        _content = _content with { Attachments = [.. _content.Attachments ?? [], .. attachments] };
        return this;
    }

    /// <inheritdoc/>
    public Models.Correspondence Build()
    {
        NotNullOrEmpty(_resourceId, "Resource ID is required");
        NotNullOrEmpty(_sender, "Sender is required");
        NotNullOrEmpty(_sendersReference, "Senders reference is required");
        NotNullOrEmpty(_content, "Content is required");
        NotNullOrEmpty(_allowSystemDeleteAfter, "AllowSystemDeleteAfter is required");
        NotNullOrEmpty(_dueDateTime, "DueDateTime is required");
        NotNullOrEmpty(_recipients, "Recipients is required");

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
            MessageSender = _messageSender,
            ExternalReferences = _externalReferences,
            PropertyList = _propertyList,
            ReplyOptions = _replyOptions,
            Notification = _notification,
            IgnoreReservation = _ignoreReservation,
            ExistingAttachments = _existingAttachments
        };
    }
}
