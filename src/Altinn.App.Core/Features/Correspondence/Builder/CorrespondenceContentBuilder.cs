using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceContent"/> objects
/// </summary>
public class CorrespondenceContentBuilder : CorrespondenceBuilderBase, ICorrespondenceContentBuilder
{
    private string? _title;
    private LanguageCode<Iso6391>? _language;
    private string? _summary;
    private string? _body;

    private CorrespondenceContentBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceContentBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceContentBuilderTitle Create() => new CorrespondenceContentBuilder();

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderLanguage WithTitle(string title)
    {
        NotNullOrEmpty(title, "Title cannot be empty");
        _title = title;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderSummary WithLanguage(LanguageCode<Iso6391> language)
    {
        NotNullOrEmpty(language, "Language cannot be empty");
        _language = language;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderSummary WithLanguage(string language)
    {
        NotNullOrEmpty(language, "Language cannot be empty");
        _language = LanguageCode<Iso6391>.Parse(language);
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilderBody WithSummary(string summary)
    {
        NotNullOrEmpty(summary, "Summary cannot be empty");
        _summary = summary;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceContentBuilder WithBody(string body)
    {
        NotNullOrEmpty(body, "Body cannot be empty");
        _body = body;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceContent Build()
    {
        NotNullOrEmpty(_title);
        NotNullOrEmpty(_language);
        NotNullOrEmpty(_summary);
        NotNullOrEmpty(_body);

        return new CorrespondenceContent
        {
            Title = _title,
            Language = _language.Value,
            Summary = _summary,
            Body = _body
        };
    }
}
