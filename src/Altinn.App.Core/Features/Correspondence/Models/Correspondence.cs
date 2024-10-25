using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

internal interface ICorrespondence
{
    void Serialize(MultipartFormDataContent content);
}

internal interface ICorrespondenceItem
{
    void Serialize(MultipartFormDataContent content, int index);
}

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
}

/// <summary>
/// Domain model for a Correspondence message
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
    public required IReadOnlyList<string> Recipients { get; init; }

    // TODO: This may not be implemented. Verify, then update xml docs (Re: Celine @ Team Melding)
    /// <summary>
    /// An alternative name for the sender of the correspondence. The name will be displayed instead of the organisation name
    /// </summary>
    public string? MessageSender { get; init; }

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

    public void Serialize(MultipartFormDataContent content)
    {
        string sender = Sender.Get(OrganisationNumberFormat.International);

        content.Add(new StringContent(ResourceId), "Correspondence.ResourceId");
        content.Add(new StringContent(sender), "Correspondence.Sender");
        content.Add(new StringContent(SendersReference), "Correspondence.SendersReference");
        content.Add(new StringContent(AllowSystemDeleteAfter.ToString("O")), "Correspondence.AllowSystemDeleteAfter");
        AddIfNotNull(content, MessageSender, "Correspondence.MessageSender");
        AddIfNotNull(content, RequestedPublishTime?.ToString("O"), "Correspondence.RequestedPublishTime");
        AddIfNotNull(content, DueDateTime.ToString("O"), "Correspondence.DueDateTime");
        AddIfNotNull(content, IgnoreReservation?.ToString(), "Correspondence.IgnoreReservation");
        AddListItems(content, Recipients, x => x, i => $"Correspondence.Recipients[{i}]");
        AddListItems(content, ExistingAttachments, x => x.ToString(), i => $"Correspondence.ExistingAttachments[{i}]");
        AddDictionaryItems(content, PropertyList, x => x, key => $"Correspondence.PropertyList.{key}");

        Content.Serialize(content);
        Notification?.Serialize(content);
        SerializeListItems(content, ExternalReferences);
        SerializeListItems(content, ReplyOptions);
    }
}

public sealed record CorrespondenceNotification : CorrespondenceBase, ICorrespondence
{
    /// <summary>
    ///
    /// </summary>
    public string NotificationTemplate { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? EmailSubject { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? EmailBody { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? SmsBody { get; init; }

    /// <summary>
    ///
    /// </summary>
    public bool? SendReminder { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? ReminderEmailSubject { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? ReminderEmailBody { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? ReminderSmsBody { get; init; }

    /// <summary>
    ///
    /// </summary>
    public NotificationChannel? NotificationChannel { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? ReminderNotificationChannel { get; init; }

    /// <summary>
    ///
    /// </summary>
    public string? SendersReference { get; init; }

    /// <summary>
    ///
    /// </summary>
    public DateTimeOffset? RequestedSendTime { get; init; }

    public void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(NotificationTemplate), "Correspondence.Notification.NotificationTemplate");
        AddIfNotNull(content, EmailSubject, "Correspondence.Notification.EmailSubject");
        AddIfNotNull(content, EmailBody, "Correspondence.Notification.EmailBody");
        AddIfNotNull(content, SmsBody, "Correspondence.Notification.SmsBody");
        AddIfNotNull(content, SendReminder?.ToString(), "Correspondence.Notification.SendReminder");
        AddIfNotNull(content, ReminderEmailSubject, "Correspondence.Notification.ReminderEmailSubject");
        AddIfNotNull(content, ReminderEmailBody, "Correspondence.Notification.ReminderEmailBody");
        AddIfNotNull(content, ReminderSmsBody, "Correspondence.Notification.ReminderSmsBody");
        AddIfNotNull(content, NotificationChannel.ToString(), "Correspondence.Notification.NotificationChannel");
        AddIfNotNull(content, ReminderNotificationChannel, "Correspondence.Notification.ReminderNotificationChannel");
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
    public required string Language { get; init; }

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

    public void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(Language), "Correspondence.Content.Language");
        content.Add(new StringContent(Title), "Correspondence.Content.MessageTitle");
        content.Add(new StringContent(Summary), "Correspondence.Content.MessageSummary");
        content.Add(new StringContent(Body), "Correspondence.Content.MessageBody");
        SerializeListItems(content, Attachments);
    }
}

/// <summary>
/// Domain model for attachment
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

    public void Serialize(MultipartFormDataContent content, int index)
    {
        const string typePrefix = "Correspondence.Content.Attachments";
        string prefix = $"{typePrefix}[{index}]";
        string sender = Sender.Get(OrganisationNumberFormat.International);

        content.Add(new StringContent(Filename), $"{prefix}.FileName");
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
