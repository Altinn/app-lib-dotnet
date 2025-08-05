namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents an attachment to a correspondence with streaming data support.
/// Inherits from CorrespondenceAttachment and provides a Stream-based data property.
/// </summary>
public record CorrespondenceStreamedAttachment : CorrespondenceBaseAttachment
{
    /// <summary>
    /// The data content as a stream.
    /// </summary>
    public required Stream Data { get; init; }

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
