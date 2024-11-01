using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Base functionality for correspondence models
/// </summary>
public abstract record CorrespondenceBase
{
    internal static void AddRequired(MultipartFormDataContent content, string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new CorrespondenceValueException($"Required value is missing: {name}");

        content.Add(new StringContent(value), name);
    }

    internal static void AddRequired(MultipartFormDataContent content, Stream data, string name, string filename)
    {
        if (data is null)
            throw new CorrespondenceValueException($"Required value is missing: {name}");

        content.Add(new StreamContent(data), name, filename);
    }

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

// TODO: It's a bit annoying that this model is named the same as the containing namespace...
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
    public IReadOnlyList<CorrespondenceReplyOption>? ReplyOptions { get; init; }

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

    // TODO: Should this be internal?
    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content)
    {
        AddRequired(content, ResourceId, "Correspondence.ResourceId");
        AddRequired(content, Sender.Get(OrganisationNumberFormat.International), "Correspondence.Sender");
        AddRequired(content, SendersReference, "Correspondence.SendersReference");
        AddRequired(content, AllowSystemDeleteAfter.ToString("O"), "Correspondence.AllowSystemDeleteAfter");
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
