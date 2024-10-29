using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceContent"/> objects
/// </summary>
public class CorrespondenceContentBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceContentBuilderTitle,
        ICorrespondenceContentBuilderLanguage,
        ICorrespondenceContentBuilderSummary,
        ICorrespondenceContentBuilderBody,
        ICorrespondenceContentBuilderBuild
{
    private string? _title;
    private LanguageCode<ISO_639_1>? _language;
    private string? _summary;
    private string? _body;
    private IReadOnlyList<CorrespondenceAttachment>? _attachments;

    private CorrespondenceContentBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceContentBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceContentBuilderTitle Create() => new CorrespondenceContentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderLanguage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderSummary WithLanguage(LanguageCode<ISO_639_1> language)
    {
        _language = language;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBody WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithBody(string body)
    {
        _body = body;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithAttachment(CorrespondenceAttachment attachment)
    {
        return WithAttachments([attachment]);
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithAttachment(ICorrespondenceAttachmentBuilderBuild builder)
    {
        return WithAttachments([builder.Build()]);
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBuild WithAttachments(IReadOnlyList<CorrespondenceAttachment> attachments)
    {
        _attachments = [.. _attachments ?? [], .. attachments];
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceContent Build()
    {
        NotNull(_title, "Title is required");
        NotNull(_language, "Language is required");
        NotNull(_summary, "Summary is required");
        NotNull(_body, "Body is required");

        return new CorrespondenceContent
        {
            Title = _title,
            Language = _language.Value,
            Summary = _summary,
            Body = _body,
            Attachments = _attachments
        };
    }
}
