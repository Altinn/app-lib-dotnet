using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceReplyOptions"/> objects
/// </summary>
public class CorrespondenceReplyOptionsBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceReplyOptionsBuilderLinkUrl,
        ICorrespondenceReplyOptionsBuilderBuild
{
    private string? _linkUrl;
    private string? _linkText;

    private CorrespondenceReplyOptionsBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceReplyOptionsBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceReplyOptionsBuilderLinkUrl Create() => new CorrespondenceReplyOptionsBuilder();

    /// <inheritdoc/>
    public ICorrespondenceReplyOptionsBuilderBuild WithLinkUrl(string linkUrl)
    {
        _linkUrl = linkUrl;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceReplyOptionsBuilderBuild WithLinkText(string linkText)
    {
        _linkText = linkText;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceReplyOptions Build()
    {
        NotNull(_linkUrl, "Link URL is required");

        return new CorrespondenceReplyOptions { LinkUrl = _linkUrl, LinkText = _linkText };
    }
}
