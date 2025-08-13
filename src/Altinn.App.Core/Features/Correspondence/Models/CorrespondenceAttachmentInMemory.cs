namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents an attachment to a correspondence.
/// </summary>
public record CorrespondenceAttachmentInMemory : CorrespondenceAttachment
{
    /// <summary>
    /// The data content.
    /// </summary>
    public required ReadOnlyMemory<byte> Data { get; init; }

    internal override void Serialise(MultipartFormDataContent content, int index, string? filenameOverride = null)
    {
        const string typePrefix = "Correspondence.Content.Attachments";
        string prefix = $"{typePrefix}[{index}]";
        string actualFilename = filenameOverride ?? Filename;

        AddRequired(content, actualFilename, $"{prefix}.Filename");
        AddRequired(content, SendersReference, $"{prefix}.SendersReference");
        AddRequired(content, DataLocationType.ToString(), $"{prefix}.DataLocationType");
        AddRequired(content, Data, "Attachments", actualFilename);
        AddIfNotNull(content, IsEncrypted?.ToString(), $"{prefix}.IsEncrypted");
    }
}
