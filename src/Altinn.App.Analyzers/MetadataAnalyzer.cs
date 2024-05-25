using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace Altinn.App.Analyzers;

internal readonly record struct MetadataAnalyzerContext(
    CompilationAnalysisContext CompilationAnalysisContext,
    string ProjectDir,
    Action? OnApplicationMetadataReadBefore,
    Action? OnApplicationMetadataDeserializationBefore
)
{
    public void ReportDiagnostic(Diagnostic diagnostic) => CompilationAnalysisContext.ReportDiagnostic(diagnostic);
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MetadataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    public Action? OnCompilationBefore { get; set; }
    public Action? OnApplicationMetadataReadBefore { get; set; }
    public Action? OnApplicationMetadataDeserializationBefore { get; set; }

    public override void Initialize(AnalysisContext context)
    {
        var configFlags = GeneratedCodeAnalysisFlags.None;
        context.ConfigureGeneratedCodeAnalysis(configFlags);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(OnCompilation);
    }

    private void OnCompilation(CompilationAnalysisContext compilationAnalysisContext)
    {
        try
        {
            OnCompilationBefore?.Invoke();

            var globalOptions = compilationAnalysisContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (!globalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                compilationAnalysisContext.ReportDiagnostic(
                    Diagnostic.Create(Diagnostics.ProjectNotFound, Location.None)
                );
                return;
            }

            var context = new MetadataAnalyzerContext(
                compilationAnalysisContext,
                projectDir,
                OnApplicationMetadataReadBefore,
                OnApplicationMetadataDeserializationBefore
            );

            AnalyzeApplicationMetadata(in context);
        }
        catch (Exception ex)
        {
            compilationAnalysisContext.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.UnknownError, Location.None, ex.Message, ex.StackTrace)
            );
        }
    }

    private static void AnalyzeApplicationMetadata(in MetadataAnalyzerContext context)
    {
        var metadataResult = ApplicationMetadataFileReader.Read(in context);
        switch (metadataResult)
        {
            case ApplicationMetadataResult.Content result:
                AnalyzeApplicationMetadataContent(in context, result);
                break;
            case ApplicationMetadataResult.FileNotFound result:
                context.ReportDiagnostic(
                    Diagnostic.Create(Diagnostics.ApplicationMetadataFileNotFound, Location.None, result.FilePath)
                );
                break;
            case ApplicationMetadataResult.CouldNotReadFile result:
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.ApplicationMetadataFileNotReadable,
                        GetLocation(result.FilePath),
                        result.Exception.Message,
                        result.Exception.StackTrace
                    )
                );
                break;
            case ApplicationMetadataResult.CouldNotParse result:
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.FailedToParseApplicationMetadata,
                        GetLocation(result.FilePath, result.SourceText),
                        result.Exception.Message,
                        result.Exception.StackTrace
                    )
                );
                break;
        }
    }

    private static void AnalyzeApplicationMetadataContent(
        in MetadataAnalyzerContext context,
        ApplicationMetadataResult.Content content
    )
    {
        var (metadata, sourceText, filePath) = content;
        var analysisContext = context.CompilationAnalysisContext;

        foreach (var dataType in metadata.DataTypes.Value)
        {
            if (dataType.AppLogic is not ParsedJsonValue<AppLogicInfo> appLogic)
                continue;

            var (classRef, lineInfo) = appLogic.Value.ClassRef;

            var classRefSymbol = analysisContext.Compilation.GetTypeByMetadataName(classRef);

            if (classRefSymbol is null)
            {
                analysisContext.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.DataTypeClassRefInvalid,
                        GetLocation(filePath, classRef, lineInfo, sourceText),
                        classRef,
                        dataType.Id.Value
                    )
                );
            }
        }
    }

    private static Location GetLocation(string file)
    {
        return Location.Create(
            file,
            TextSpan.FromBounds(1, 2),
            new LinePositionSpan(LinePosition.Zero, LinePosition.Zero)
        );
    }

    private static Location GetLocation(string file, SourceText sourceText)
    {
        var lines = sourceText.Lines;
        var textSpan = TextSpan.FromBounds(0, lines[lines.Count - 1].End);
        return Location.Create(file, textSpan, lines.GetLinePositionSpan(textSpan));
    }

    private static Location GetLocation(string file, string value, IJsonLineInfo lineInfo, SourceText sourceText)
    {
        var lines = sourceText.Lines;
        var line = lines[lineInfo.LineNumber - 1];
        var textSpan = TextSpan.FromBounds(
            line.Start + lineInfo.LinePosition - value.Length - 1,
            line.Start + lineInfo.LinePosition - 1
        );

        var lineSpan = lines.GetLinePositionSpan(textSpan);
        return Location.Create(file, textSpan, lineSpan);
    }
}
