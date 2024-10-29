using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceReplyOptionsBuilder"/> instance is on the <see cref="CorrespondenceReplyOptions.LinkUrl"/> step
/// </summary>
public interface ICorrespondenceReplyOptionsBuilderLinkUrl
{
    /// <summary>
    /// Sets the link URL for the reply options
    /// </summary>
    /// <param name="linkUrl">The link URL</param>
    ICorrespondenceReplyOptionsBuilderBuild WithLinkUrl(string linkUrl);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceReplyOptionsBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceReplyOptionsBuilder.Build"/>
/// </summary>
public interface ICorrespondenceReplyOptionsBuilderBuild
{
    /// <summary>
    /// Sets the link text for the reply options
    /// </summary>
    /// <param name="linkText">The link text</param>
    ICorrespondenceReplyOptionsBuilderBuild WithLinkText(string linkText);

    /// <summary>
    /// Builds the <see cref="CorrespondenceReplyOptions"/> instance
    /// </summary>
    CorrespondenceReplyOptions Build();
}
