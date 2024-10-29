using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Title"/> step
/// </summary>
public interface ICorrespondenceContentBuilderTitle
{
    /// <summary>
    /// Sets the title of the correspondence content
    /// </summary>
    /// <param name="title">The correspondence title</param>
    ICorrespondenceContentBuilderLanguage WithTitle(string title);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Language"/> step
/// </summary>
public interface ICorrespondenceContentBuilderLanguage
{
    /// <summary>
    /// Sets the language of the correspondence content
    /// </summary>
    /// <param name="language"></param>
    ICorrespondenceContentBuilderSummary WithLanguage(LanguageCode<ISO_639_1> language);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Summary"/> step
/// </summary>
public interface ICorrespondenceContentBuilderSummary
{
    /// <summary>
    /// Sets the summary of the correspondence content
    /// </summary>
    /// <param name="summary">The summary of the message</param>
    ICorrespondenceContentBuilderBody WithSummary(string summary);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance is on the <see cref="CorrespondenceContent.Body"/> step
/// </summary>
public interface ICorrespondenceContentBuilderBody
{
    /// <summary>
    /// Sets the body of the correspondence content
    /// </summary>
    /// <param name="body">The full text (body) of the message</param>
    ICorrespondenceContentBuilderBuild WithBody(string body);
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceContentBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceContentBuilder.Build"/>
/// </summary>
public interface ICorrespondenceContentBuilderBuild
{
    /// <summary>
    /// Adds an attachment to the correspondence content
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/></remarks>
    /// </summary>
    /// <param name="attachment">A <see cref="CorrespondenceAttachment"/> item</param>
    ICorrespondenceContentBuilderBuild WithAttachment(CorrespondenceAttachment attachment);

    /// <summary>
    /// Adds an attachment to the correspondence content
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/>
    /// </remarks>
    /// </summary>
    /// <param name="builder">A <see cref="CorrespondenceAttachmentBuilder"/> instance in the <see cref="ICorrespondenceAttachmentBuilderBuild"/> stage</param>
    ICorrespondenceContentBuilderBuild WithAttachment(ICorrespondenceAttachmentBuilderBuild builder);

    /// <summary>
    /// Adds attachments to the correspondence content
    /// <remarks>
    /// This method respects any existing attachments already stored in <see cref="CorrespondenceContent.Attachments"/></remarks>
    /// </summary>
    /// <param name="attachments">A List of <see cref="CorrespondenceAttachment"/> items</param>
    ICorrespondenceContentBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments);

    /// <summary>
    /// Builds the correspondence content
    /// </summary>
    CorrespondenceContent Build();
}
