namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents a base attachment of a correspondence.
/// </summary>
public abstract record CorrespondenceBaseAttachment : MultipartCorrespondenceItem
{
    /// <summary>
    /// The filename of the attachment.
    /// </summary>
    public required string Filename { get; init; }

    /// <summary>
    /// A value indicating whether the attachment is encrypted or not.
    /// </summary>
    public bool? IsEncrypted { get; init; }

    /// <summary>
    /// A reference value given to the attachment by the creator.
    /// </summary>
    public required string SendersReference { get; init; }

    /// <summary>
    /// Specifies the storage location of the attachment data.
    /// </summary>
    public CorrespondenceDataLocationType DataLocationType { get; init; } =
        CorrespondenceDataLocationType.ExistingCorrespondenceAttachment;

    /// <summary>
    /// Serialise method
    /// </summary>
    internal abstract void Serialise(MultipartFormDataContent content, int index, string? filenameOverride = null);
}
