using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

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
    /// A reference value given to the attachment by the creator
    /// </summary>
    public required string SendersReference { get; init; }

    /// <summary>
    /// The attachment data type in MIME format
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Specifies the storage location of the attachment data
    /// </summary>
    public CorrespondenceDataLocationType DataLocationType { get; init; } =
        CorrespondenceDataLocationType.ExistingCorrespondenceAttachment;

    /// <summary>
    /// The file stream
    /// </summary>
    public required Stream Data { get; init; }

    /// <summary>
    /// If duplicate attachment filenames are detected during serialization,
    /// this field is populated with a unique index, which is in turn used by <see cref="UniqueFileName"/>
    /// </summary>
    internal int? FilenameClashUniqueId;

    // TODO: Should this be internal?
    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        const string typePrefix = "Correspondence.Content.Attachments";
        string prefix = $"{typePrefix}[{index}]";

        AddRequired(content, UniqueFileName(), $"{prefix}.Filename");
        AddRequired(content, Name, $"{prefix}.Name");
        AddRequired(content, SendersReference, $"{prefix}.SendersReference");
        AddRequired(content, DataType, $"{prefix}.DataType");
        AddRequired(content, DataLocationType.ToString(), $"{prefix}.DataLocationType");
        AddRequired(content, Data, $"{prefix}.Attachments", UniqueFileName());
        AddIfNotNull(content, IsEncrypted?.ToString(), $"{prefix}.IsEncrypted");

        // NOTE: RestrictionName can't be omitted or empty, but it may be irrelevant to most callers.
        // Default to FileName if value is missing.
        string restrictionName = string.IsNullOrWhiteSpace(RestrictionName) ? Filename : RestrictionName;
        AddRequired(content, restrictionName, $"{prefix}.RestrictionName");
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
