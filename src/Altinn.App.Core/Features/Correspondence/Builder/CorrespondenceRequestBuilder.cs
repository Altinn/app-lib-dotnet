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
    ///
    /// Additionally this method checks for empty strings and emtpy data allocations.
    /// </summary>
    /// <param name="value">The value to assert</param>
    /// <param name="errorMessage">The error message to throw, if the value was null</param>
    /// <exception cref="CorrespondenceValueException"></exception>
    internal static void NotNullOrEmpty([NotNull] object? value, string? errorMessage = null)
    {
        if (
            value is null
            || value is string str && string.IsNullOrWhiteSpace(str)
            || value is ReadOnlyMemory<byte> { IsEmpty: true }
        )
        {
            throw new CorrespondenceValueException(errorMessage);
        }
    }
}

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceRequest"/> objects
/// </summary>
public class CorrespondenceRequestBuilder : CorrespondenceBuilderBase, ICorrespondenceRequestBuilder
{
    private string? _resourceId;
    private OrganisationNumber? _sender;
    private string? _sendersReference;
    private CorrespondenceContent? _content;
    private DateTimeOffset? _allowSystemDeleteAfter;
    private DateTimeOffset? _dueDateTime;
    private IReadOnlyList<OrganisationOrPersonIdentifier>? _recipients;
    private DateTimeOffset? _requestedPublishTime;
    private string? _messageSender;
    private IReadOnlyList<CorrespondenceExternalReference>? _externalReferences;
    private IReadOnlyDictionary<string, string>? _propertyList;
    private IReadOnlyList<CorrespondenceReplyOption>? _replyOptions;
    private CorrespondenceNotification? _notification;
    private bool? _ignoreReservation;
    private IReadOnlyList<Guid>? _existingAttachments;

    private CorrespondenceRequestBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceRequestBuilder"/> instance
    /// </summary>
    public static ICorrespondenceRequestBuilderResourceId Create() => new CorrespondenceRequestBuilder();

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderSender WithResourceId(string resourceId)
    {
        NotNullOrEmpty(resourceId, "Resource ID cannot be empty");
        _resourceId = resourceId;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderSendersReference WithSender(OrganisationNumber sender)
    {
        NotNullOrEmpty(sender, "Sender cannot be empty");
        _sender = sender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderSendersReference WithSender(string sender)
    {
        NotNullOrEmpty(sender, "Sender cannot be empty");
        _sender = OrganisationNumber.Parse(sender);
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderRecipients WithSendersReference(string sendersReference)
    {
        NotNullOrEmpty(sendersReference, "Senders reference cannot be empty");
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderAllowSystemDeleteAfter WithRecipient(OrganisationOrPersonIdentifier recipient)
    {
        return WithRecipients([recipient]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderAllowSystemDeleteAfter WithRecipient(string recipient)
    {
        return WithRecipients([recipient]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderAllowSystemDeleteAfter WithRecipients(
        IReadOnlyList<OrganisationOrPersonIdentifier> recipients
    )
    {
        _recipients = [.. _recipients ?? [], .. recipients];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderAllowSystemDeleteAfter WithRecipients(IReadOnlyList<string> recipients)
    {
        NotNullOrEmpty(recipients);

        _recipients = [.. _recipients ?? [], .. recipients.Select(OrganisationOrPersonIdentifier.Parse)];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilderContent WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter)
    {
        NotNullOrEmpty(allowSystemDeleteAfter, "AllowSystemDeleteAfter cannot be empty");
        _allowSystemDeleteAfter = allowSystemDeleteAfter;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithContent(CorrespondenceContent content)
    {
        NotNullOrEmpty(content, "Content cannot be empty");
        _content = content;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithContent(ICorrespondenceContentBuilder builder)
    {
        return WithContent(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithDueDateTime(DateTimeOffset dueDateTime)
    {
        NotNullOrEmpty(dueDateTime, "DueDateTime cannot be empty");
        _dueDateTime = dueDateTime;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithRequestedPublishTime(DateTimeOffset requestedPublishTime)
    {
        _requestedPublishTime = requestedPublishTime;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithMessageSender(string messageSender)
    {
        _messageSender = messageSender;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithExternalReference(CorrespondenceExternalReference externalReference)
    {
        return WithExternalReferences([externalReference]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithExternalReference(ICorrespondenceExternalReferenceBuilder builder)
    {
        return WithExternalReferences([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference> externalReferences
    )
    {
        _externalReferences = [.. _externalReferences ?? [], .. externalReferences];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithPropertyList(IReadOnlyDictionary<string, string> propertyList)
    {
        _propertyList = propertyList;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithReplyOption(CorrespondenceReplyOption replyOption)
    {
        return WithReplyOptions([replyOption]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithReplyOption(ICorrespondenceReplyOptionsBuilder builder)
    {
        return WithReplyOptions([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithReplyOptions(IReadOnlyList<CorrespondenceReplyOption> replyOptions)
    {
        _replyOptions = [.. _replyOptions ?? [], .. replyOptions];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithNotification(CorrespondenceNotification notification)
    {
        _notification = notification;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithNotification(ICorrespondenceNotificationBuilder builder)
    {
        return WithNotification(builder.Build());
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithIgnoreReservation(bool ignoreReservation)
    {
        _ignoreReservation = ignoreReservation;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithExistingAttachment(Guid existingAttachment)
    {
        return WithExistingAttachments([existingAttachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithExistingAttachments(IReadOnlyList<Guid> existingAttachments)
    {
        _existingAttachments = [.. _existingAttachments ?? [], .. existingAttachments];
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithAttachment(CorrespondenceAttachment attachment)
    {
        return WithAttachments([attachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithAttachment(ICorrespondenceAttachmentBuilder builder)
    {
        return WithAttachments([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceRequestBuilder WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments)
    {
        NotNullOrEmpty(_content, "Content is required before adding attachments");

        _content = _content with { Attachments = [.. _content.Attachments ?? [], .. attachments] };
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceRequest Build()
    {
        NotNullOrEmpty(_resourceId);
        NotNullOrEmpty(_sender);
        NotNullOrEmpty(_sendersReference);
        NotNullOrEmpty(_content);
        NotNullOrEmpty(_allowSystemDeleteAfter);
        NotNullOrEmpty(_recipients);

        return new CorrespondenceRequest
        {
            ResourceId = _resourceId,
            Sender = _sender.Value,
            SendersReference = _sendersReference,
            Content = _content,
            AllowSystemDeleteAfter = _allowSystemDeleteAfter.Value,
            DueDateTime = _dueDateTime,
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
