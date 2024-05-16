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
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ProjectNotFoundError, Location.None));
            return;
        }

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        // On compilation end, it is not totally unreasonable to read a file
        var file = Path.Combine(projectDir, "config/applicationmetadata.json");
        if (!File.Exists(file))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.ApplicationMetadataFileNotFoundError, Location.None)
            );
            return;
        }

        try
        {
            var jsonText = File.ReadAllText(file);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
            var metadata = JObject.Parse(jsonText) ?? throw new Exception("Deserialization returned 'null'");
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
                    // TODO: create location
                    // Location.Create(file, TextSpan.FromBounds(..), LinePositionSpan);
                    context.ReportDiagnostic(
                        Diagnostic.Create(Diagnostics.DataTypeClassRefInvalidError, Location.None, classRef, dataTypeId)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Diagnostics.FailedToParseApplicationMetadataError,
                    Location.None,
                    ex.Message,
                    ex.StackTrace
                )
            );
            return;
        }
    }
}
