using System.Text;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.App.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MetadataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        // var configFlags = GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics;
        var configFlags = GeneratedCodeAnalysisFlags.None;
        context.ConfigureGeneratedCodeAnalysis(configFlags);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(OnCompilation);
        // System.Diagnostics.Debugger.Launch();
    }

    private void OnCompilation(CompilationAnalysisContext context)
    {
        if (
            !context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                "build_property.projectdir",
                out var projectDir
            )
        )
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ProjectNotFound, Location.None));
            return;
        }

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        // On compilation end, it is not totally unreasonable to read a file
        // TODO: in the future, it would be nice if app projects used `<AdditionalFiles .. />`
        var file = Path.Combine(projectDir, "config/applicationmetadata.json");
        if (!File.Exists(file))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ApplicationMetadataFileNotFound, Location.None));
            return;
        }

        SourceText? sourceText = null;
        try
        {
            var jsonText = File.ReadAllText(file);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
            sourceText = SourceText.From(jsonText, Encoding.UTF8);

            using var reader = new JsonTextReader(new StringReader(jsonText));

            JsonLoadSettings loadSettings = new JsonLoadSettings()
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Load
            };

            var metadata = JObject.Load(reader, loadSettings);
            var dataTypes =
                (metadata.GetValue("dataTypes") as JArray) ?? throw new JsonException("Failed to parse 'dataTypes'");

            foreach (var dataTypeToken in dataTypes)
            {
                var dataType = (dataTypeToken as JObject) ?? throw new JsonException("Could not parse 'dataType'");
                var dataTypeId =
                    (dataType.GetValue("id") as JValue)?.Value as string
                    ?? throw new JsonException("Could not parse 'id' from 'dataType'");

                var appLogic = dataType.GetValue("appLogic") as JObject;
                if (appLogic is null)
                    continue;

                var classRefToken = appLogic.GetValue("classRef");
                var classRef =
                    (classRefToken as JValue)?.Value as string ?? throw new JsonException("Could not parse 'classRef'");
                var classRefSymbol = context.Compilation.GetTypeByMetadataName(classRef);

                if (classRefSymbol is null)
                {
                    if (classRefToken is null)
                        throw new JsonException("classRef was null");
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.DataTypeClassRefInvalid,
                            GetLocation(file, classRef, classRefToken, sourceText),
                            classRef,
                            dataTypeId
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.FailedToParseApplicationMetadata,
                    sourceText is not null ? GetLocation(file, sourceText) : Location.None,
                    ex.Message,
                    ex.StackTrace
                )
            );
            return;
        }
    }

    private static Location GetLocation(string file, SourceText sourceText)
    {
        var lines = sourceText.Lines;
        var textSpan = TextSpan.FromBounds(0, lines[lines.Count - 1].End);
        return Location.Create(file, textSpan, lines.GetLinePositionSpan(textSpan));
    }

    private static Location GetLocation(string file, string value, JToken token, SourceText sourceText)
    {
        var lineInfo = token as IJsonLineInfo;
        if (lineInfo is null || lineInfo.HasLineInfo() is false)
            throw new Exception("Could not get LineInfo from token in JSON");

        var lines = sourceText.Lines;
        var line = lines[lineInfo.LineNumber - 1];
        var textSpan = TextSpan.FromBounds(
            line.Start + lineInfo.LinePosition - value.Length - 1,
            line.Start + lineInfo.LinePosition - 1
        );

        return Location.Create(file, textSpan, lines.GetLinePositionSpan(textSpan));
    }
}
