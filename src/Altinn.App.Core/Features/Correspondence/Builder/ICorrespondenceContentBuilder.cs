using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Title"/> step
/// </summary>
public interface ICorrespondenceContentBuilderNeedsTitle
{
    /// <summary>
    /// Sets the title of the correspondence content
    /// </summary>
    /// <param name="title">The correspondence title</param>
    ICorrespondenceContentBuilderNeedsLanguage WithTitle(string title);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Language"/> step
/// </summary>
public interface ICorrespondenceContentBuilderNeedsLanguage
{
    /// <summary>
    /// Sets the language of the correspondence content
    /// </summary>
    /// <param name="language"></param>
    ICorrespondenceContentBuilderNeedsSummary WithLanguage(LanguageCode<Iso6391> language);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Summary"/> step
/// </summary>
public interface ICorrespondenceContentBuilderNeedsSummary
{
    /// <summary>
    /// Sets the summary of the correspondence content
    /// </summary>
    /// <param name="summary">The summary of the message</param>
    ICorrespondenceContentBuilderNeedsBody WithSummary(string summary);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Body"/> step
/// </summary>
public interface ICorrespondenceContentBuilderNeedsBody
{
    /// <summary>
    /// Sets the body of the correspondence content
    /// </summary>
    /// <param name="body">The full text (body) of the message</param>
    ICorrespondenceContentBuilderCanBuild WithBody(string body);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceContentBuilder.Build"/>
/// </summary>
public interface ICorrespondenceContentBuilderCanBuild
{
    /// <summary>
    /// Builds the correspondence content
    /// </summary>
    CorrespondenceContent Build();
}
