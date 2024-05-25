using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.Analyzers;

internal abstract record ApplicationMetadataResult
{
    private ApplicationMetadataResult() { }

    public sealed record Content(ApplicationMetadata Value, SourceText SourceText, string FilePath)
        : ApplicationMetadataResult;

    public sealed record FileNotFound(string FilePath) : ApplicationMetadataResult;

    public sealed record CouldNotReadFile(Exception Exception, string FilePath) : ApplicationMetadataResult;

    public sealed record CouldNotParse(Exception Exception, SourceText SourceText, string FilePath)
        : ApplicationMetadataResult;
}

internal static class ApplicationMetadataFileReader
{
    public static readonly string ApplicationMetadataFilePath = Path.Combine("config", "applicationmetadata.json");

    public static ApplicationMetadataResult Read(in MetadataAnalyzerContext context)
    {
        var file = Path.Combine(context.ProjectDir, ApplicationMetadataFilePath);

        SourceText sourceText;
        string jsonText;
        try
        {
            context.OnApplicationMetadataReadBefore?.Invoke();

            if (!File.Exists(file))
                return new ApplicationMetadataResult.FileNotFound(file);

            jsonText = File.ReadAllText(file, Encoding.UTF8);
            sourceText = SourceText.From(jsonText, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return new ApplicationMetadataResult.CouldNotReadFile(ex, file);
        }

        ApplicationMetadata metadata;
        try
        {
            context.OnApplicationMetadataDeserializationBefore?.Invoke();

            metadata = ApplicationMetadataJsonReader.Read(jsonText);
        }
        catch (Exception ex)
        {
            return new ApplicationMetadataResult.CouldNotParse(ex, sourceText, file);
        }

        return new ApplicationMetadataResult.Content(metadata, sourceText, file);
    }
}
