using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Domain model for a Correspondence message
/// </summary>
public sealed record CorrespondenceMessage
{
    /// <summary>
    /// The Resource Id for the correspondence service
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>
    /// The sending organisation of the correspondence
    /// </summary>
    public required OrganisationNumber Sender { get; init; }

    /// <summary></summary>
    public string SendersReference { get; init; }

    /// <summary></summary>
    public CorrespondenceMessageContent Content { get; init; }

    /// <summary></summary>
    public DateTimeOffset? RequestedPublishTime { get; init; }

    /// <summary></summary>
    public DateTimeOffset? AllowSystemDeleteAfter { get; init; }

    /// <summary></summary>
    public DateTimeOffset? DueDateTime { get; init; }

    /// <summary></summary>
    public IReadOnlyList<string> Recipients { get; init; }

    /// <summary></summary>
    public string? MessageSender { get; init; }

    /// <summary></summary>
    public IReadOnlyList<CorrespondenceExternalReference>? ExternalReferences { get; init; }

    /// <summary></summary>
    public IReadOnlyDictionary<string, string>? PropertyList { get; init; }

    /// <summary></summary>
    public IReadOnlyList<CorrespondenceReplyOptions>? ReplyOptions { get; init; }

    /// <summary></summary>
    public CorrespondenceNotification? Notification { get; init; }

    /// <summary></summary>
    public bool? IgnoreReservation { get; init; }

    /// <summary></summary>
    public IReadOnlyList<Guid>? ExistingAttachments { get; init; }

    internal void Serialize(MultipartFormDataContent multipartContent)
    {
        var sender = Sender.Get(OrganisationNumberFormat.International);

        multipartContent.Add(new StringContent(ResourceId), "Correspondence.ResourceId");
        multipartContent.Add(new StringContent(sender), "Correspondence.Sender");
        multipartContent.Add(new StringContent(SendersReference), "Correspondence.SendersReference");
        if (!string.IsNullOrWhiteSpace(MessageSender))
            multipartContent.Add(new StringContent(MessageSender), "Correspondence.MessageSender");

        Content.Serialize(multipartContent);

        for (int i = 0; i < Recipients.Count; i++)
        {
            multipartContent.Add(new StringContent(Recipients[i]), $"Correspondence.Recipients[{i}]");
        }

        if (RequestedPublishTime is not null)
            multipartContent.Add(
                new StringContent(RequestedPublishTime.Value.ToString("O")),
                "Correspondence.RequestedPublishTime"
            );
        if (AllowSystemDeleteAfter is not null)
            multipartContent.Add(
                new StringContent(AllowSystemDeleteAfter.Value.ToString("O")),
                "Correspondence.AllowSystemDeleteAfter"
            );
        if (DueDateTime is not null)
            multipartContent.Add(new StringContent(DueDateTime.Value.ToString("O")), "Correspondence.DueDateTime");

        if (ExternalReferences?.Count > 0)
        {
            for (int i = 0; i < ExternalReferences?.Count; i++)
            {
                ExternalReferences[i].Serialize(multipartContent, i);
            }
        }

        if (PropertyList?.Count > 0)
        {
            foreach (var (key, value) in PropertyList)
            {
                multipartContent.Add(new StringContent(value), $"Correspondence.PropertyList.{key}");
            }
        }

        if (ReplyOptions?.Count > 0)
        {
            for (int i = 0; i < ReplyOptions.Count; i++)
            {
                ReplyOptions[i].Serialize(multipartContent, i);
            }
        }

        if (Notification is not null)
            Notification.Serialize(multipartContent);

        if (IgnoreReservation is not null)
            multipartContent.Add(
                new StringContent(IgnoreReservation.Value.ToString()),
                "Correspondence.IgnoreReservation"
            );

        if (ExistingAttachments?.Count > 0)
        {
            for (int i = 0; i < ExistingAttachments.Count; i++)
            {
                multipartContent.Add(
                    new StringContent(ExistingAttachments[i].ToString()),
                    $"Correspondence.ExistingAttachments[{i}]"
                );
            }
        }
    }
}

public sealed record CorrespondenceNotification(
    string NotificationTemplate,
    string? EmailSubject,
    string? EmailBody,
    string? SmsBody,
    bool? SendReminder,
    string? ReminderEmailSubject,
    string? ReminderEmailBody,
    string? ReminderSmsBody,
    string? NotificationChannel,
    string? ReminderNotificationChannel,
    string? SendersReference,
    DateTimeOffset? RequestedSendTime
)
{
    internal void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(NotificationTemplate), "Correspondence.Notification.NotificationTemplate");
        TryAddField(content, "Correspondence.Notification.EmailSubject", EmailSubject);
        TryAddField(content, "Correspondence.Notification.EmailBody", EmailBody);
        TryAddField(content, "Correspondence.Notification.SmsBody", SmsBody);
        TryAddField(content, "Correspondence.Notification.SendReminder", SendReminder?.ToString());
        TryAddField(content, "Correspondence.Notification.ReminderEmailSubject", ReminderEmailSubject);
        TryAddField(content, "Correspondence.Notification.ReminderEmailBody", ReminderEmailBody);
        TryAddField(content, "Correspondence.Notification.ReminderSmsBody", ReminderSmsBody);
        TryAddField(content, "Correspondence.Notification.NotificationChannel", NotificationChannel);
        TryAddField(content, "Correspondence.Notification.ReminderNotificationChannel", ReminderNotificationChannel);
        TryAddField(content, "Correspondence.Notification.SendersReference", SendersReference);
        TryAddField(content, "Correspondence.Notification.RequestedSendTime", RequestedSendTime?.ToString("O"));

        static void TryAddField(MultipartFormDataContent content, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                content.Add(new StringContent(value), name);
        }
    }
}

/// <summary>
///
/// </summary>
/// <param name="ReferenceType"></param>
/// <param name="ReferenceValue"></param>
public sealed record CorrespondenceExternalReference(string ReferenceType, string ReferenceValue)
{
    internal void Serialize(MultipartFormDataContent content, int i)
    {
        content.Add(new StringContent(ReferenceType), $"Correspondence.ExternalReferences[{i}].ReferenceType");
        content.Add(new StringContent(ReferenceValue), $"Correspondence.ExternalReferences[{i}].ReferenceValue");
    }
}

/// <summary>
///
/// </summary>
/// <param name="LinkUrl"></param>
/// <param name="LinkText"></param>
public sealed record CorrespondenceReplyOptions(string LinkUrl, string LinkText)
{
    internal void Serialize(MultipartFormDataContent content, int i)
    {
        content.Add(new StringContent(LinkUrl), $"Correspondence.ReplyOptions[{i}].LinkUrl");
        content.Add(new StringContent(LinkText), $"Correspondence.ReplyOptions[{i}].LinkText");
    }
}

/// <summary>
/// Domain model for a Correspondence message content
/// </summary>
/// <param name="Title"></param>
/// <param name="Language"></param>
/// <param name="Summary"></param>
/// <param name="Body"></param>
/// <param name="Attachments"></param>
public sealed record CorrespondenceMessageContent(
    string Title,
    string Language,
    string Summary,
    string Body,
    IReadOnlyList<CorrespondenceAttachment>? Attachments
)
{
    internal void Serialize(MultipartFormDataContent content)
    {
        content.Add(new StringContent(Language), "Correspondence.Content.Language");
        content.Add(new StringContent(Title), "Correspondence.Content.MessageTitle");
        content.Add(new StringContent(Summary), "Correspondence.Content.MessageSummary");
        content.Add(new StringContent(Body), "Correspondence.Content.MessageBody");

        if (Attachments is not null)
        {
            // Ensure uniuque filenames
            var hasDuplicateFilenames = Attachments
                .GroupBy(x => x.Filename.ToLowerInvariant())
                .Where(x => x.Count() > 1);

            foreach (var duplicates in hasDuplicateFilenames)
            {
                var attachments = duplicates.ToList();
                for (int i = 0; i < attachments.Count; i++)
                {
                    attachments[i].FilenameClashUniqueId = i + 1;
                }
            }

            // Serialize
            for (int i = 0; i < Attachments.Count; i++)
            {
                var attachment = Attachments[i];
                attachment.Serialize(content, i);
            }
        }
    }
}

/// <summary>
/// Domain model for attachment
/// </summary>
public sealed record CorrespondenceAttachment
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

    internal int? FilenameClashUniqueId { get; set; }

    internal void Serialize(MultipartFormDataContent content, int i)
    {
        const string typePrefix = "Correspondence.Content.Attachments";

        var prefix = $"{typePrefix}[{i}]";
        content.Add(new StringContent(Filename), $"{prefix}.FileName");
        content.Add(new StringContent(Name), $"{prefix}.Name");
        content.Add(new StringContent(Sender.Get(OrganisationNumberFormat.International)), $"{prefix}.Sender");
        content.Add(new StringContent(SendersReference), $"{prefix}.SendersReference");
        content.Add(new StringContent(DataType), $"{prefix}.DataType");
        content.Add(new StringContent(DataLocationType.ToString()), $"{prefix}.DataLocationType");
        content.Add(new StreamContent(Data), $"{prefix}.Attachments", UniqueFileName());

        if (IsEncrypted is not null)
            content.Add(new StringContent(IsEncrypted.Value.ToString()), $"{prefix}.IsEncrypted");

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
