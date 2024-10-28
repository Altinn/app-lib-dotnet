using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

internal interface ICorrespondence
{
    /// <summary>
    /// Serialize the correspondence to a <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    void Serialize(MultipartFormDataContent content);
}

internal interface ICorrespondenceItem
{
    /// <summary>
    /// Serialize each correspondence item to a <see cref="MultipartFormDataContent"/> instance
    /// </summary>
    /// <param name="content"></param>
    /// <param name="index"></param>
    void Serialize(MultipartFormDataContent content, int index);
}

/// <summary>
/// Base functionality for correspondence models
/// </summary>
public abstract record CorrespondenceBase
{
    internal static void AddIfNotNull(MultipartFormDataContent content, string? value, string name)
    {
        if (!string.IsNullOrWhiteSpace(value))
            content.Add(new StringContent(value), name);
    }

    internal static void AddListItems<T>(
        MultipartFormDataContent content,
        IReadOnlyList<T>? items,
        Func<T, string> valueFactory,
        Func<int, string> keyFactory
    )
    {
        if (IsEmptyCollection(items))
            return;

        for (int i = 0; i < items.Count; i++)
        {
            string key = keyFactory.Invoke(i);
            string value = valueFactory.Invoke(items[i]);
            content.Add(new StringContent(value), key);
        }
    }

    internal static void SerializeListItems<T>(MultipartFormDataContent content, IReadOnlyList<T>? items)
        where T : ICorrespondenceItem
    {
        if (IsEmptyCollection(items))
            return;

        // Ensure unique filenames for attachments
        if (items is IReadOnlyList<CorrespondenceAttachment> attachments)
        {
            var hasDuplicateFilenames = attachments
                .GroupBy(x => x.Filename.ToLowerInvariant())
                .Where(x => x.Count() > 1)
                .Select(x => x.ToList());

            foreach (var duplicates in hasDuplicateFilenames)
            {
                for (int i = 0; i < duplicates.Count; i++)
                {
                    duplicates[i].FilenameClashUniqueId = i + 1;
                }
            }
        }

        // Serialize
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Serialize(content, i);
        }
    }

    internal static void AddDictionaryItems<TKey, TValue>(
        MultipartFormDataContent content,
        IReadOnlyDictionary<TKey, TValue>? items,
        Func<TValue, string> valueFactory,
        Func<TKey, string> keyFactory
    )
    {
        if (IsEmptyCollection(items))
            return;

        foreach (var (dictKey, dictValue) in items)
        {
            string key = keyFactory.Invoke(dictKey);
            string value = valueFactory.Invoke(dictValue);
            content.Add(new StringContent(value), key);
        }
    }

    private static bool IsEmptyCollection<T>([NotNullWhen(false)] IReadOnlyCollection<T>? collection)
    {
        return collection is null || collection.Count == 0;
    }

    internal void ValidateAllProperties(string dataTypeName)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(this);
        bool isValid = Validator.TryValidateObject(
            this,
            validationContext,
            validationResults,
            validateAllProperties: true
        );

        if (isValid is false)
        {
            throw new CorrespondenceValueException(
                $"Validation failed for {dataTypeName}",
                new AggregateException(validationResults.Select(x => new ValidationException(x.ErrorMessage)))
            );
        }
    }
}

/// <summary>
/// Data model for an Altinn correspondence
/// </summary>
public sealed record Correspondence : CorrespondenceBase, ICorrespondence
{
    /// <summary>
    /// The Resource Id for the correspondence service
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    /// The sending organisation of the correspondence
    /// </summary>
    public required OrganisationNumber Sender { get; init; }

    /// <summary>
    /// A reference value given to the message by the creator
    /// </summary>
    public required string SendersReference { get; init; }

    /// <summary>
    /// The content of the message
    /// </summary>
    public required CorrespondenceContent Content { get; init; }

    /// <summary>
    /// When should the correspondence become visible to the recipient?
    /// If omitted, the correspondence is available immediately
    /// </summary>
    public DateTimeOffset? RequestedPublishTime { get; init; }

    /// <summary>
    /// When can Altinn remove the correspondence from its database?
    /// </summary>
    public required DateTimeOffset AllowSystemDeleteAfter { get; init; }

    // TODO: This is currently required, but should be optional. Await correspondence team
    /// <summary>
    /// When must the recipient respond by?
    /// </summary>
    public required DateTimeOffset DueDateTime { get; init; }

    /// <summary>
    /// The recipients of the correspondence
    /// </summary>
    public required IReadOnlyList<OrganisationNumber> Recipients { get; init; }

    // TODO: This is not fully implemented by Altinn Correspondence yet (Re: Celine @ Team Melding)
    /*
    /// <summary>
    /// An alternative name for the sender of the correspondence. The name will be displayed instead of the organisation name
    /// </summary>
    public string? MessageSender { get; init; }
    */

    /// <summary>
    /// Reference to other items in the Altinn ecosystem
    /// </summary>
    public IReadOnlyList<CorrespondenceExternalReference>? ExternalReferences { get; init; }

    /// <summary>
    /// User-defined properties related to the correspondence
    /// </summary>
    public IReadOnlyDictionary<string, string>? PropertyList { get; init; }

    /// <summary>
    /// Options for how the recipient can reply to the correspondence
    /// </summary>
    public IReadOnlyList<CorrespondenceReplyOptions>? ReplyOptions { get; init; }

    /// <summary>
    /// Notifications associated with this correspondence
    /// </summary>
    public CorrespondenceNotification? Notification { get; init; }

    /// <summary>
    /// Specifies whether the correspondence can override reservation against digital comminication in KRR
    /// </summary>
    public bool? IgnoreReservation { get; init; }

    /// <summary>
    /// Existing attachments that should be added to the correspondence
    /// </summary>
    public IReadOnlyList<Guid>? ExistingAttachments { get; init; }

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(ResourceId), "Correspondence.ResourceId");
        content.Add(new StringContent(Sender.Get(OrganisationNumberFormat.International)), "Correspondence.Sender");
        content.Add(new StringContent(SendersReference), "Correspondence.SendersReference");
        content.Add(new StringContent(AllowSystemDeleteAfter.ToString("O")), "Correspondence.AllowSystemDeleteAfter");
        //AddIfNotNull(content, MessageSender, "Correspondence.MessageSender");
        AddIfNotNull(content, RequestedPublishTime?.ToString("O"), "Correspondence.RequestedPublishTime");
        AddIfNotNull(content, DueDateTime.ToString("O"), "Correspondence.DueDateTime");
        AddIfNotNull(content, IgnoreReservation?.ToString(), "Correspondence.IgnoreReservation");
        AddListItems(
            content,
            Recipients,
            x => x.Get(OrganisationNumberFormat.International),
            i => $"Correspondence.Recipients[{i}]"
        );
        AddListItems(content, ExistingAttachments, x => x.ToString(), i => $"Correspondence.ExistingAttachments[{i}]");
        AddDictionaryItems(content, PropertyList, x => x, key => $"Correspondence.PropertyList.{key}");

        Content.Serialize(content);
        Notification?.Serialize(content);
        SerializeListItems(content, ExternalReferences);
        SerializeListItems(content, ReplyOptions);
    }
}

/// <summary>
/// Represents a notification to be sent to the recipient of a correspondence
/// </summary>
public sealed record CorrespondenceNotification : CorrespondenceBase, ICorrespondence
{
    /// <summary>
    /// The notification template for use for notifications
    /// </summary>
    public required NotificationTemplate NotificationTemplate { get; init; }

    /// <summary>
    /// The email subject to use for notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(128, MinimumLength = 0)]
    public string? EmailSubject { get; init; }

    /// <summary>
    /// The email body content to use for notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(1024, MinimumLength = 0)]
    public string? EmailBody { get; init; }

    /// <summary>
    /// The sms content to use for notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(160, MinimumLength = 0)]
    public string? SmsBody { get; init; }

    /// <summary>
    /// Should a reminder be send if this correspondence has not been actioned within an appropriate timeframe?
    /// </summary>
    public bool? SendReminder { get; init; }

    /// <summary>
    /// The email subject to use for reminder notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(128, MinimumLength = 0)]
    public string? ReminderEmailSubject { get; init; }

    /// <summary>
    /// The email body content to use for reminder notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(1024, MinimumLength = 0)]
    public string? ReminderEmailBody { get; init; }

    /// <summary>
    /// The sms content to use for reminder notifications
    /// <remarks>
    /// Depending on the <see cref="NotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// </summary>
    [StringLength(160, MinimumLength = 0)]
    public string? ReminderSmsBody { get; init; }

    /// <summary>
    /// Where should the notifications be sent?
    /// </summary>
    public NotificationChannel? NotificationChannel { get; init; }

    /// <summary>
    /// Where should the reminder notifications be sent?
    /// </summary>
    public NotificationChannel? ReminderNotificationChannel { get; init; }

    /// <summary>
    /// Senders reference for this notification
    /// </summary>
    public string? SendersReference { get; init; }

    /// <summary>
    /// The date and time for when the notification should be sent
    /// </summary>
    public DateTimeOffset? RequestedSendTime { get; init; }

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content)
    {
        ValidateAllProperties(nameof(CorrespondenceNotification));

        content.Add(
            new StringContent(NotificationTemplate.ToString()),
            "Correspondence.Notification.NotificationTemplate"
        );
        AddIfNotNull(content, EmailSubject, "Correspondence.Notification.EmailSubject");
        AddIfNotNull(content, EmailBody, "Correspondence.Notification.EmailBody");
        AddIfNotNull(content, SmsBody, "Correspondence.Notification.SmsBody");
        AddIfNotNull(content, SendReminder?.ToString(), "Correspondence.Notification.SendReminder");
        AddIfNotNull(content, ReminderEmailSubject, "Correspondence.Notification.ReminderEmailSubject");
        AddIfNotNull(content, ReminderEmailBody, "Correspondence.Notification.ReminderEmailBody");
        AddIfNotNull(content, ReminderSmsBody, "Correspondence.Notification.ReminderSmsBody");
        AddIfNotNull(content, NotificationChannel.ToString(), "Correspondence.Notification.NotificationChannel");
        AddIfNotNull(
            content,
            ReminderNotificationChannel.ToString(),
            "Correspondence.Notification.ReminderNotificationChannel"
        );
        AddIfNotNull(content, SendersReference, "Correspondence.Notification.SendersReference");
        AddIfNotNull(content, RequestedSendTime?.ToString("O"), "Correspondence.Notification.RequestedSendTime");
    }
}

/// <summary>
/// Represents a reference to another item in the Altinn ecosystem
/// </summary>
public sealed record CorrespondenceExternalReference : CorrespondenceBase, ICorrespondenceItem
{
    /// <summary>
    /// The reference type
    /// </summary>
    public required string ReferenceType { get; init; }

    /// <summary>
    /// The reference value
    /// </summary>
    public required string ReferenceValue { get; init; }

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        content.Add(new StringContent(ReferenceType), $"Correspondence.ExternalReferences[{index}].ReferenceType");
        content.Add(new StringContent(ReferenceValue), $"Correspondence.ExternalReferences[{index}].ReferenceValue");
    }
}

/// <summary>
/// Methods for recipients to respond to a correspondence, in additon to the normal Read and Confirm operations
/// </summary>
public sealed record CorrespondenceReplyOptions : CorrespondenceBase, ICorrespondenceItem
{
    /// <summary>
    /// The URL to be used as a reply/response to a correspondence
    /// </summary>
    public required string LinkUrl { get; init; }

    /// <summary>
    /// The link text
    /// </summary>
    public string? LinkText { get; init; }

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        content.Add(new StringContent(LinkUrl), $"Correspondence.ReplyOptions[{index}].LinkUrl");
        AddIfNotNull(content, LinkText, $"Correspondence.ReplyOptions[{index}].LinkText");
    }
}

/// <summary>
/// The message content in a correspondence
/// </summary>
public sealed record CorrespondenceContent : CorrespondenceBase, ICorrespondence
{
    /// <summary>
    /// The correspondence message title (subject)
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The language of the correspondence, specified according to ISO 639-1
    /// </summary>
    public required LanguageCode<ISO_639_1> Language { get; init; }

    /// <summary>
    /// The summary text of the correspondence message
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// The full text (body) of the correspondence message
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// File attachments to associate with this correspondence
    /// </summary>
    public IReadOnlyList<CorrespondenceAttachment>? Attachments { get; init; }

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(Language.Get()), "Correspondence.Content.Language");
        content.Add(new StringContent(Title), "Correspondence.Content.MessageTitle");
        content.Add(new StringContent(Summary), "Correspondence.Content.MessageSummary");
        content.Add(new StringContent(Body), "Correspondence.Content.MessageBody");
        SerializeListItems(content, Attachments);
    }
}

/// <summary>
/// Represents an attachment to a correspondence
/// </summary>
public sealed record CorrespondenceAttachment : CorrespondenceBase, ICorrespondenceItem
{
    /// <summary>
    /// The filename of the attachment
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// The display name of the attachment
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The name of the Restriction Policy restricting access to this element
    /// </summary>
    /// <remarks>
    /// An empty value indicates no restriction beyond the ones governing the correspondence referencing this attachment
    /// </remarks>
    public string? RestrictionName { get; init; }

    /// <summary>
    /// A value indicating whether the attachment is encrypted or not
    /// </summary>
    public bool? IsEncrypted { get; init; }

    /// <summary>
    /// The sending organisation of the attachment
    /// </summary>
    public required OrganisationNumber Sender { get; init; }

    /// <summary>
    /// A reference value given to the attachment by the creator
    /// </summary>
    public required string SendersReference { get; init; }

    /// <summary>
    /// The attachment data type in MIME format
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Specifies the location of the attachment data
    /// </summary>
    public DataLocationType DataLocationType { get; init; } = DataLocationType.ExistingCorrespondenceAttachment;

    /// <summary>
    /// The file stream
    /// </summary>
    public required Stream Data { get; init; }

    /// <summary>
    /// If duplicate attachment filenames are detected during serialization,
    /// this field is populated with a unique index, which is in turn used by <see cref="UniqueFileName"/>
    /// </summary>
    internal int? FilenameClashUniqueId;

    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        const string typePrefix = "Correspondence.Content.Attachments";
        string prefix = $"{typePrefix}[{index}]";
        string sender = Sender.Get(OrganisationNumberFormat.International);

        content.Add(new StringContent(UniqueFileName()), $"{prefix}.FileName");
        content.Add(new StringContent(Name), $"{prefix}.Name");
        content.Add(new StringContent(sender), $"{prefix}.Sender");
        content.Add(new StringContent(SendersReference), $"{prefix}.SendersReference");
        content.Add(new StringContent(DataType), $"{prefix}.DataType");
        content.Add(new StringContent(DataLocationType.ToString()), $"{prefix}.DataLocationType");
        content.Add(new StreamContent(Data), $"{prefix}.Attachments", UniqueFileName());
        AddIfNotNull(content, IsEncrypted?.ToString(), $"{prefix}.IsEncrypted");

        // NOTE: RestrictionName can't be omitted or empty, but it may be irrelevant to most callers.
        // Default to FileName if value is missing.
        content.Add(
            new StringContent(string.IsNullOrWhiteSpace(RestrictionName) ? Filename : RestrictionName),
            $"{prefix}.RestrictionName"
        );
    }

    private string UniqueFileName()
    {
        if (FilenameClashUniqueId is null)
        {
            return Filename;
        }

        string filename = Path.GetFileNameWithoutExtension(Filename);
        string extension = Path.GetExtension(Filename);
        return $"{filename}({FilenameClashUniqueId}).{extension}";
    }
}

public interface ICorrespondenceBuilderResourceId
{
    /// <summary>
    /// Set the Resource Id for the correspondence
    /// </summary>
    /// <param name="resourceId">The resource ID as registered in the Altinn Resource Registry</param>
    ICorrespondenceBuilderContent WithResourceId(string resourceId);
}

public interface ICorrespondenceBuilderContent
{
    /// <summary>
    /// Set the content of the correspondence
    /// </summary>
    /// <param name="content">The correspondence content</param>
    ICorrespondenceBuilderSender WithContent(CorrespondenceContent content);
}

public interface ICorrespondenceBuilderSender
{
    /// <summary>
    /// Set the sender of the correspondence
    /// </summary>
    /// <param name="sender">The correspondence sender</param>
    ICorrespondenceBuilderSendersReference WithSender(OrganisationNumber sender);
}

public interface ICorrespondenceBuilderSendersReference
{
    /// <summary>
    /// Set the senders reference for the correspondence
    /// </summary>
    /// <param name="sendersReference">The correspondence reference</param>
    ICorrespondenceBuilderRecipients WithSendersReference(string sendersReference);
}

public interface ICorrespondenceBuilderRecipients
{
    /// <summary>
    /// Set the recipients of the correspondence
    /// </summary>
    /// <param name="recipients">A list of recipients</param>
    ICorrespondenceBuilderDueDateTime WithRecipients(IReadOnlyList<OrganisationNumber> recipients);
}

public interface ICorrespondenceBuilderDueDateTime
{
    /// <summary>
    /// Set due date and time for the correspondence
    /// </summary>
    /// <param name="dueDateTime">The point in time when the correspondence is due</param>
    /// <returns></returns>
    ICorrespondenceBuilderAllowSystemDeleteAfter WithDueDateTime(DateTimeOffset dueDateTime);
}

public interface ICorrespondenceBuilderAllowSystemDeleteAfter
{
    /// <summary>
    /// Set the date and time when the correspondence can be deleted from the system
    /// </summary>
    /// <param name="allowSystemDeleteAfter">The point in time when the correspondence may be safely deleted</param>
    ICorrespondenceBuilderBuild WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter);
}

public interface ICorrespondenceBuilderBuild
{
    /// <summary>
    /// Set the requested publish time for the correspondence
    /// </summary>
    /// <param name="requestedPublishTime">The point in time when the correspondence should be published</param>
    ICorrespondenceBuilderBuild WithRequestedPublishTime(DateTimeOffset? requestedPublishTime);

    // TODO: This is not fully implemented by Altinn Correspondence yet (Re: Celine @ Team Melding)
    /*
    /// <summary>
    /// Set the message sender for the correspondence
    /// </summary>
    /// <param name="messageSender">The name of the message sender</param>
    /// <returns></returns>
    ICorrespondenceBuilderBuild WithMessageSender(string? messageSender);
    */

    /// <summary>
    /// Set the external references for the correspondence
    /// </summary>
    /// <param name="externalReferences">A list of reference to other items in the Altinn ecosystem</param>
    ICorrespondenceBuilderBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference>? externalReferences
    );

    /// <summary>
    /// Set the property list for the correspondence
    /// </summary>
    /// <param name="propertyList">A key-value list of arbitrary properties to associate with the correspondence</param>
    ICorrespondenceBuilderBuild WithPropertyList(IReadOnlyDictionary<string, string>? propertyList);

    /// <summary>
    /// Set the reply options for the correspondence
    /// </summary>
    /// <param name="replyOptions">A list of options for how the recipient can reply to the correspondence</param>
    ICorrespondenceBuilderBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOptions>? replyOptions);

    /// <summary>
    /// Set the notification for the correspondence
    /// </summary>
    /// <param name="notification">The notification details to be associated with the correspondence</param>
    ICorrespondenceBuilderBuild WithNotification(CorrespondenceNotification? notification);

    /// <summary>
    /// Set whether the correspondence can override reservation against digital communication in KRR
    /// </summary>
    /// <param name="ignoreReservation">A boolean value indicating whether or not reservations can be ignored</param>
    ICorrespondenceBuilderBuild WithIgnoreReservation(bool? ignoreReservation);

    /// <summary>
    /// Set the existing attachments that should be added to the correspondence
    /// </summary>
    /// <param name="existingAttachments">A list of <see cref="Guid"/>s pointing to existing attachments</param>
    ICorrespondenceBuilderBuild WithExistingAttachments(IReadOnlyList<Guid>? existingAttachments);

    /// <summary>
    /// Add attachments for the correspondence
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="attachments">A List of <see cref="CorrespondenceAttachment"/> items</param>
    ICorrespondenceBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment>? attachments);

    /// <summary>
    /// Build the correspondence
    /// </summary>
    Correspondence Build();
}

public class CorrespondenceBuilder
    : ICorrespondenceBuilderResourceId,
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
    /// Create a new <see cref="CorrespondenceBuilder"/> instance
    /// </summary>
    public static ICorrespondenceBuilderResourceId Create() => new CorrespondenceBuilder();

    /// <inheritdoc/>
    public ICorrespondenceBuilderContent WithResourceId(string resourceId)
    {
        _resourceId = resourceId;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderSender WithContent(CorrespondenceContent content)
    {
        _content = content;
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
    public ICorrespondenceBuilderBuild WithAllowSystemDeleteAfter(DateTimeOffset allowSystemDeleteAfter)
    {
        _allowSystemDeleteAfter = allowSystemDeleteAfter;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithRequestedPublishTime(DateTimeOffset? requestedPublishTime)
    {
        _requestedPublishTime = requestedPublishTime;
        return this;
    }

    // TODO: This is not fully implemented by Altinn Correspondence yet (Re: Celine @ Team Melding)
    /*
    /// <inheritdoc/>
    public IBuildStep WithMessageSender(string? messageSender)
    {
        _messageSender = messageSender;
        return this;
    }
    */

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithExternalReferences(
        IReadOnlyList<CorrespondenceExternalReference>? externalReferences
    )
    {
        _externalReferences = externalReferences;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithPropertyList(IReadOnlyDictionary<string, string>? propertyList)
    {
        _propertyList = propertyList;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithReplyOptions(IReadOnlyList<CorrespondenceReplyOptions>? replyOptions)
    {
        _replyOptions = replyOptions;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithNotification(CorrespondenceNotification? notification)
    {
        _notification = notification;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithIgnoreReservation(bool? ignoreReservation)
    {
        _ignoreReservation = ignoreReservation;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithExistingAttachments(IReadOnlyList<Guid>? existingAttachments)
    {
        _existingAttachments = existingAttachments;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment>? attachments)
    {
        // Because of the interface-chaining in this builder, `content` is guaranteed to be non-null here.
        // But compiler doesn't trust that, so we add this check.
        if (_content is null)
            throw new CorrespondenceValueException("Content is required before adding attachments");

        _content = _content with { Attachments = [.. _content.Attachments ?? [], .. attachments] };
        return this;
    }

    /// <inheritdoc/>
    public Correspondence Build()
    {
        if (_resourceId is null)
            throw new CorrespondenceValueException("Resource ID is required");

        if (_sender is null)
            throw new CorrespondenceValueException("Sender is required");

        if (_sendersReference is null)
            throw new CorrespondenceValueException("Senders reference is required");

        if (_content is null)
            throw new CorrespondenceValueException("Content is required");

        if (_allowSystemDeleteAfter is null)
            throw new CorrespondenceValueException("Allow system delete after is required");

        if (_dueDateTime is null)
            throw new CorrespondenceValueException("Due date time is required");

        if (_recipients is null)
            throw new CorrespondenceValueException("Recipients is required");

        return new Correspondence
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

public interface ICorrespondenceContentBuilderTitle
{
    /// <summary>
    /// Set the title of the correspondence content
    /// </summary>
    /// <param name="title">The correspondence title</param>
    ICorrespondenceContentBuilderLanguage WithTitle(string title);
}

public interface ICorrespondenceContentBuilderLanguage
{
    /// <summary>
    /// Set the language of the correspondence content
    /// </summary>
    /// <param name="language"></param>
    ICorrespondenceContentBuilderSummary WithLanguage(LanguageCode<ISO_639_1> language);
}

public interface ICorrespondenceContentBuilderSummary
{
    /// <summary>
    /// Set the summary of the correspondence content
    /// </summary>
    /// <param name="summary">The summary of the message</param>
    ICorrespondenceContentBuilderBody WithSummary(string summary);
}

public interface ICorrespondenceContentBuilderBody
{
    /// <summary>
    /// Set the body of the correspondence content
    /// </summary>
    /// <param name="body">The full text (body) of the message</param>
    ICorrespondenceContentBuilderBuild WithBody(string body);
}

public interface ICorrespondenceContentBuilderBuild
{
    /// <summary>
    /// Adds attachments to the correspondence content
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/></remarks>
    /// </summary>
    /// <param name="attachments">A List of <see cref="CorrespondenceAttachment"/> items</param>
    ICorrespondenceContentBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment>? attachments);

    /// <summary>
    /// Build the correspondence content
    /// </summary>
    CorrespondenceContent Build();
}

public class CorrespondenceContentBuilder
    : ICorrespondenceContentBuilderTitle,
        ICorrespondenceContentBuilderLanguage,
        ICorrespondenceContentBuilderSummary,
        ICorrespondenceContentBuilderBody,
        ICorrespondenceContentBuilderBuild
{
    private string? _title;
    private LanguageCode<ISO_639_1>? _language;
    private string? _summary;
    private string? _body;

    private IReadOnlyList<CorrespondenceAttachment>? _attachments;
    private IReadOnlyDictionary<string, string>? _metadata;

    private CorrespondenceContentBuilder() { }

    /// <summary>
    /// Create a new <see cref="CorrespondenceContentBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceContentBuilderTitle Create() => new CorrespondenceContentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderLanguage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderSummary WithLanguage(LanguageCode<ISO_639_1> language)
    {
        _language = language;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBody WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithBody(string body)
    {
        _body = body;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment>? attachments)
    {
        _attachments = [.. _attachments ?? [], .. attachments];
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceContent Build()
    {
        if (_title is null)
            throw new CorrespondenceValueException("Title is required");

        if (_language is null)
            throw new CorrespondenceValueException("Language is required");

        if (_summary is null)
            throw new CorrespondenceValueException("Summary is required");

        if (_body is null)
            throw new CorrespondenceValueException("Body is required");

        return new CorrespondenceContent
        {
            Title = _title,
            Language = _language.Value,
            Summary = _summary,
            Body = _body,
            Attachments = _attachments
        };
    }
}

public class Tester
{
    public void Test()
    {
        var builder = CorrespondenceBuilder
            .Create()
            .WithResourceId("123")
            .WithContent(
                CorrespondenceContentBuilder
                    .Create()
                    .WithTitle("Title")
                    .WithLanguage(LanguageCode<ISO_639_1>.Parse("enc"))
                    .WithSummary("Summary")
                    .WithBody("Body")
                    .Build()
            )
            .WithSender(OrganisationNumber.Parse("123456789"))
            .WithSendersReference("123")
            .WithRecipients([OrganisationNumber.Parse("987654321")])
            .WithDueDateTime(DateTimeOffset.Now)
            .WithAllowSystemDeleteAfter(DateTimeOffset.Now.AddDays(30))
            .Build();
    }
}



/// <summary>
///
/// </summary>
/// <param name="Title"></param>
/// <param name="Language"></param>
/// <param name="Summary"></param>
/// <param name="Body"></param>
// public sealed record MessageContentBuilder(string Title, string Language, string Summary, string Body)
// {
//     public IReadOnlyList<CorrespondenceAttachment>? Attachments { get; init; }

//     public MessageContentBuilder WithAttachments(params IReadOnlyList<CorrespondenceAttachment> attachments) =>
//         this with
//         {
//             Attachments = [.. Attachments, .. attachments]
//         };

//     public MessageContent Build() => new(Title, Language, Summary, Body, Attachments);
// }

// /// <summary>
// ///
// /// </summary>
// /// <param name="ResourceId"></param>
// /// <param name="Sender"></param>
// /// <param name="SendersReference"></param>
// /// <param name="Content"></param>
// /// <param name="Recipients"></param>
// public sealed record CorrespondenceMessageBuilder(
//     string ResourceId,
//     // Sender org number
//     OrganisationNumber Sender,
//     string SendersReference,
//     MessageContentBuilder Content,
//     IReadOnlyList<string> Recipients
// )
// {
//     public DateTimeOffset? RequestedPublishTime { get; init; }
//     public DateTimeOffset? AllowSystemDeleteAfter { get; init; }
//     public DateTimeOffset? DueDateTime { get; init; }
//     public IReadOnlyList<string>? ExternalReferences { get; init; }
//     public string? MessageSender { get; init; }

//     public CorrespondenceMessageBuilder WithRequestedPublishTime(DateTimeOffset requestedPublishTime) =>
//         this with
//         {
//             RequestedPublishTime = requestedPublishTime
//         };

//     public CorrespondenceMessageBuilder WithContentAttachments(
//         params IReadOnlyList<CorrespondenceAttachment> attachments
//     ) => this with { Content = Content.WithAttachments(attachments) };

//     public CorrespondenceMessage Build() =>
//         new(
//             ResourceId,
//             Sender,
//             SendersReference,
//             Content.Build(),
//             Recipients,
//             RequestedPublishTime,
//             AllowSystemDeleteAfter,
//             DueDateTime,
//             ExternalReferences,
//             MessageSender
//         );
// }
