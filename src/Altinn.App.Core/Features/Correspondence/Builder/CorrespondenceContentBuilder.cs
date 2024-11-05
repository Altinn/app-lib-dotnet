using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceContent"/> objects
/// </summary>
public class CorrespondenceContentBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceContentBuilderNeedsTitle,
        ICorrespondenceContentBuilderNeedsLanguage,
        ICorrespondenceContentBuilderNeedsSummary,
        ICorrespondenceContentBuilderNeedsBody,
        ICorrespondenceContentBuilderCanBuild
{
    private string? _title;
    private LanguageCode<ISO_639_1>? _language;
    private string? _summary;
    private string? _body;

    private CorrespondenceContentBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceContentBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceContentBuilderNeedsTitle Create() => new CorrespondenceContentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderNeedsLanguage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderNeedsSummary WithLanguage(LanguageCode<ISO_639_1> language)
    {
        _language = language;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderNeedsBody WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderCanBuild WithBody(string body)
    {
        _body = body;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceContent Build()
    {
        NotNullOrEmpty(_title, "Title is required");
        NotNullOrEmpty(_language, "Language is required");
        NotNullOrEmpty(_summary, "Summary is required");
        NotNullOrEmpty(_body, "Body is required");

        return new CorrespondenceContent
        {
            Title = _title,
            Language = _language.Value,
            Summary = _summary,
            Body = _body
        };
    }
}
