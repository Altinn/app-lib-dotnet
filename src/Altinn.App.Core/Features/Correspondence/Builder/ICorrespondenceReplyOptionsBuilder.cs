using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceReplyOptionBuilder"/> instance is on the <see cref="CorrespondenceReplyOption.LinkUrl"/> step
/// </summary>
public interface ICorrespondenceReplyOptionsBuilderNeedsLinkUrl
{
    /// <summary>
    /// Sets the link URL for the reply options
    /// </summary>
    /// <param name="linkUrl">The link URL</param>
    ICorrespondenceReplyOptionsBuilderCanBuild WithLinkUrl(string linkUrl);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceReplyOptionBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceReplyOptionBuilder.Build"/>
/// </summary>
public interface ICorrespondenceReplyOptionsBuilderCanBuild
{
    /// <summary>
    /// Sets the link text for the reply options
    /// </summary>
    /// <param name="linkText">The link text</param>
    ICorrespondenceReplyOptionsBuilderCanBuild WithLinkText(string linkText);

    /// <summary>
    /// Builds the <see cref="CorrespondenceReplyOption"/> instance
    /// </summary>
    CorrespondenceReplyOption Build();
}
