using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Altinn.App.Internal.Analyzers;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Altinn.App.Benchmarks.Analyzers;

[Config(typeof(Config))]
public class AppImplementationFactoryAnalyzerBenchmarks
{
    private AdhocWorkspace _workspace;
    private Project _project;
    private CompilationWithAnalyzersOptions _options;
    private ImmutableArray<DiagnosticAnalyzer> _analyzers;

    [GlobalSetup]
    public void Setup()
    {
        var dir = Fixture.ProjectFolder;

        var manager = new AnalyzerManager();
        var analyzer = manager.GetProject(
            Path.Combine(dir.FullName, "..", "..", "src", "Altinn.App.Core", "Altinn.App.Core.csproj")
        );
        _workspace = analyzer.GetWorkspace();
        var solution = _workspace.CurrentSolution;
        _project = solution.Projects.Single(p => p.Name == "Altinn.App.Core");
        _analyzers = [new AppImplementationInjectionAnalyzer()];

        var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        _options = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            static (ex, analyzer, diagnostic) => throw ex,
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: true
        );
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> Analyze()
    {
        var compilation = await _project.GetCompilationAsync();
        var compilationWithAnalyzers = compilation.WithAnalyzers(_analyzers, _options);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private sealed class Config : ManualConfig
    {
        public Config()
        {
            this.SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
            this.AddDiagnoser(MemoryDiagnoser.Default);
            // this.AddDiagnoser(new DotTraceDiagnoser());
            this.AddColumn(RankColumn.Arabic);
            this.Orderer = new DefaultOrderer(SummaryOrderPolicy.SlowestToFastest, MethodOrderPolicy.Declared);
        }
    }
}
