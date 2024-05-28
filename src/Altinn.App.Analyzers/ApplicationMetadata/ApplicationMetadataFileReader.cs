using System.Text;
using Altinn.App.Analyzers.Json;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.Analyzers.ApplicationMetadata;

internal abstract record ApplicationMetadataResult
{
    private ApplicationMetadataResult() { }

    public sealed record Content(ApplicationMetadataInfo Value, SourceText SourceText, string FilePath)
        : ApplicationMetadataResult;

    public sealed record FileNotFound(string FilePath) : ApplicationMetadataResult;

    public sealed record CouldNotReadFile(Exception Exception, string FilePath) : ApplicationMetadataResult;

    public sealed record CouldNotParse(Exception Exception, SourceText SourceText, string FilePath)
        : ApplicationMetadataResult;

    public sealed record CouldNotParseField(JsonTokenDescriptor Token, SourceText SourceText, string FilePath)
        : ApplicationMetadataResult;

    public sealed record Cancelled : ApplicationMetadataResult;
}

internal static class ApplicationMetadataFileReader
{
    public static readonly string RelativeFilePath = Path.Combine("config", "applicationmetadata.json");

    public static ApplicationMetadataResult Read(in MetadataAnalyzerContext context)
    {
        var cancellationToken = context.CancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return new ApplicationMetadataResult.Cancelled();

        var file = Path.Combine(context.ProjectDir, RelativeFilePath);
        SourceText sourceText;
        string jsonText;
        try
        {
            context.OnApplicationMetadataReadBefore?.Invoke();

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
                    return new ApplicationMetadataResult.FileNotFound(file);

                jsonText = File.ReadAllText(file, Encoding.UTF8);
                sourceText = SourceText.From(jsonText, Encoding.UTF8);
            }

            if (cancellationToken.IsCancellationRequested)
                return new ApplicationMetadataResult.Cancelled();
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return new ApplicationMetadataResult.Cancelled();
            return new ApplicationMetadataResult.CouldNotReadFile(ex, file);
        }

        try
        {
            context.OnApplicationMetadataDeserializationBefore?.Invoke();

            var parseResult = ApplicationMetadataJsonReader.Read(jsonText, cancellationToken);

            return parseResult switch
            {
                ApplicationMetadataParseResult.Ok { Value: var metadata }
                    => new ApplicationMetadataResult.Content(metadata, sourceText, file),
                ApplicationMetadataParseResult.FailedToParse { Token: var token }
                    => new ApplicationMetadataResult.CouldNotParseField(token, sourceText, file),
                ApplicationMetadataParseResult.Error { Exception: var ex }
                    => new ApplicationMetadataResult.CouldNotParse(ex, sourceText, file),
                ApplicationMetadataParseResult.Cancelled => new ApplicationMetadataResult.Cancelled(),
            };
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                return new ApplicationMetadataResult.Cancelled();
            return new ApplicationMetadataResult.CouldNotParse(ex, sourceText, file);
        }
    }
}
