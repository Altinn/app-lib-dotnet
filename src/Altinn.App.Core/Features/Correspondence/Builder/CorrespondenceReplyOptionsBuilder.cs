using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceReplyOption"/> objects
/// </summary>
public class CorrespondenceReplyOptionBuilder : CorrespondenceBuilderBase, ICorrespondenceReplyOptionsBuilder
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
        NotNullOrEmpty(_linkUrl, "Link URL is required");

        return new CorrespondenceReplyOption { LinkUrl = _linkUrl, LinkText = _linkText };
    }
}
