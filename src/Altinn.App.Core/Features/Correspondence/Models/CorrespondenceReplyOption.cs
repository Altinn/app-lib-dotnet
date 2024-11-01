namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Methods for recipients to respond to a correspondence, in additon to the normal Read and Confirm operations
/// </summary>
public sealed record CorrespondenceReplyOption : CorrespondenceBase, ICorrespondenceItem
{
    /// <summary>
    /// The URL to be used as a reply/response to a correspondence
    /// </summary>
    public required string LinkUrl { get; init; }

    /// <summary>
    /// The link text
    /// </summary>
    public string? LinkText { get; init; }

    // TODO: Should this be internal?
    /// <inheritdoc />
    public void Serialize(MultipartFormDataContent content, int index)
    {
        AddRequired(content, LinkUrl, $"Correspondence.ReplyOptions[{index}].LinkUrl");
        AddIfNotNull(content, LinkText, $"Correspondence.ReplyOptions[{index}].LinkText");
    }
}
