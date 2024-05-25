using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Altinn.App.Analyzers;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Altinn.App.Benchmarks.Analyzers;

[Config(typeof(Config))]
public class AnalyzerBenchmarks
{
    private AdhocWorkspace _workspace;
    private Project _project;
    private CompilationWithAnalyzersOptions _options;
    private ImmutableArray<DiagnosticAnalyzer> _analyzers;

    [GlobalSetup]
    public void Setup()
    {
        var dir = GetTestAppDirectory();

        var manager = new AnalyzerManager();
        var analyzer = manager.GetProject(Path.Combine(dir.FullName, "App", "App.csproj"));
        var projectDir = Path.Combine(dir.FullName, "App");
        _workspace = analyzer.GetWorkspace();
        var solution = _workspace.CurrentSolution;
        _project = solution.Projects.Single(p => p.Name == "App");

        var globalOptions = new Dictionary<string, string>()
        {
            ["build_property.projectdir"] = projectDir
        }.ToImmutableDictionary();
        _options = new CompilationWithAnalyzersOptions(
            new AnalyzerOptions(
                [],
                new TestOptionsProvider(
                    ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                    new TestAnalyzerConfigOptions(globalOptions)
                )
            ),
            static (ex, analyzer, diagnostic) =>
                throw new Exception($"Analyzer exception due to {diagnostic.Id}: {ex}"),
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: true
        );
        _analyzers = [new MetadataAnalyzer()];
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> AnalyzeMetadata()
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
            this.AddDiagnoser(new DotTraceDiagnoser());
            this.AddColumn(RankColumn.Arabic);
            this.Orderer = new DefaultOrderer(SummaryOrderPolicy.SlowestToFastest, MethodOrderPolicy.Declared);
        }
    }

    private static DirectoryInfo GetTestAppDirectory()
    {
        var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var dir = new DirectoryInfo(currentDir);
        var slnFile = "AppLibDotnet.sln";
        for (int i = 0; i < 25 && dir is not null && dir.GetFiles(slnFile).Length == 0; i++)
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("Could not find root directory for repo");

        var testAppDir = Path.Combine(dir.FullName, "test", "Altinn.App.Analyzers.Tests", "testapp");
        var result = new DirectoryInfo(testAppDir);
        if (!dir.Exists)
            throw new InvalidOperationException("Could not find testapp directory");
        return result;
    }

    private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

        public static TestOptionsProvider Empty { get; } =
            new TestOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                TestAnalyzerConfigOptions.Empty
            );

        internal TestOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions
        )
        {
            _treeDict = treeDict;
            GlobalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            _treeDict.TryGetValue(tree, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            _treeDict.TryGetValue(textFile, out var options) ? options : TestAnalyzerConfigOptions.Empty;

        internal TestOptionsProvider WithAdditionalTreeOptions(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict
        ) => new TestOptionsProvider(_treeDict.AddRange(treeDict), GlobalOptions);

        internal TestOptionsProvider WithGlobalOptions(AnalyzerConfigOptions globalOptions) =>
            new TestOptionsProvider(_treeDict, globalOptions);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        internal static readonly ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<
            string,
            string
        >(KeyComparer);

        public static TestAnalyzerConfigOptions Empty { get; } = new TestAnalyzerConfigOptions(EmptyDictionary);

        internal readonly ImmutableDictionary<string, string> Options;

        public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> options) => Options = options;

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value) =>
            Options.TryGetValue(key, out value);

        public override IEnumerable<string> Keys => Options.Keys;
    }
}
