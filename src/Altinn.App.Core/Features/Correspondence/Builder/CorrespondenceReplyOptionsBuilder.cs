using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceReplyOption"/> objects
/// </summary>
public class CorrespondenceReplyOptionBuilder : ICorrespondenceReplyOptionsBuilder
{
    private string? _linkUrl;
    private string? _linkText;

    private CorrespondenceReplyOptionBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceReplyOptionBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceReplyOptionsBuilderNeedsLinkUrl Create() => new CorrespondenceReplyOptionBuilder();

    /// <inheritdoc/>
    public ICorrespondenceReplyOptionsBuilder WithLinkUrl(string linkUrl)
    {
        BuilderUtils.NotNullOrEmpty(linkUrl, "Link URL cannot be empty");
        _linkUrl = linkUrl;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceReplyOptionsBuilder WithLinkText(string linkText)
    {
        _linkText = linkText;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceReplyOption Build()
    {
        BuilderUtils.NotNullOrEmpty(_linkUrl);

        return new CorrespondenceReplyOption { LinkUrl = _linkUrl, LinkText = _linkText };
    }
}
