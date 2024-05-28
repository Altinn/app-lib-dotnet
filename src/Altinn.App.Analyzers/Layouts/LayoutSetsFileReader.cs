using System.Text;
using Altinn.App.Analyzers.Json;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.Analyzers.Layouts;

internal abstract record LayoutSetsResult
{
    private LayoutSetsResult() { }

    public sealed record Content(LayoutSetsInfo Value, SourceText SourceText, string FilePath) : LayoutSetsResult;

    public sealed record FileNotFound(string FilePath) : LayoutSetsResult;

    public sealed record CouldNotReadFile(Exception Exception, string FilePath) : LayoutSetsResult;

    public sealed record CouldNotParse(Exception Exception, SourceText SourceText, string FilePath) : LayoutSetsResult;

    public sealed record CouldNotParseField(JsonTokenDescriptor Token, SourceText SourceText, string FilePath)
        : LayoutSetsResult;

    public sealed record Cancelled : LayoutSetsResult;
}

internal static class LayoutSetsFileReader
{
    public static readonly string RelativeFilePath = Path.Combine("ui", "layout-sets.json");

    public static LayoutSetsResult Read(in MetadataAnalyzerContext context)
    {
        var cancellationToken = context.CancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return new LayoutSetsResult.Cancelled();

        var file = Path.Combine(context.ProjectDir, RelativeFilePath);
        SourceText sourceText;
        string jsonText;
        try
        {
            context.OnLayoutSetsReadBefore?.Invoke();

            var additionalFile = context.GetAdditionlFileEndingWithPath(RelativeFilePath);

            var additionalFileSourceText = additionalFile?.GetText(cancellationToken);
            if (additionalFileSourceText is not null)
            {
                sourceText = additionalFileSourceText;
                jsonText = additionalFileSourceText.ToString();
            }
            else
            {
                if (!File.Exists(file))
                    return new LayoutSetsResult.FileNotFound(file);

                jsonText = File.ReadAllText(file, Encoding.UTF8);
                sourceText = SourceText.From(jsonText, Encoding.UTF8);
            }

            if (cancellationToken.IsCancellationRequested)
                return new LayoutSetsResult.Cancelled();
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return new LayoutSetsResult.Cancelled();
            return new LayoutSetsResult.CouldNotReadFile(ex, file);
        }

        try
        {
            context.OnLayoutSetsDeserializationBefore?.Invoke();

            var parseResult = LayoutSetsJsonReader.Read(jsonText, cancellationToken);

            return parseResult switch
            {
                LayoutSetsParseResult.Ok { Value: var metadata }
                    => new LayoutSetsResult.Content(metadata, sourceText, file),
                LayoutSetsParseResult.FailedToParse { Token: var token }
                    => new LayoutSetsResult.CouldNotParseField(token, sourceText, file),
                LayoutSetsParseResult.Error { Exception: var ex }
                    => new LayoutSetsResult.CouldNotParse(ex, sourceText, file),
                LayoutSetsParseResult.Cancelled => new LayoutSetsResult.Cancelled(),
            };
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return new LayoutSetsResult.Cancelled();
            return new LayoutSetsResult.CouldNotParse(ex, sourceText, file);
        }
    }
}
