using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace Altinn.App.Analyzers.Tests.Fixtures;

public sealed class AltinnAppCoreFixture : IDisposable
{
    private ITestOutputHelper? _output;
    private string? _projectDir;
    private AdhocWorkspace? _workspace;
    private Project? _project;

    [
        MemberNotNullWhen(true, nameof(_projectDir)),
        MemberNotNullWhen(true, nameof(_workspace)),
        MemberNotNullWhen(true, nameof(_project))
    ]
    private bool _isInitialized { get; set; }

    public void SetTestOutputHelper(ITestOutputHelper output) => _output = output;

    public ITestOutputHelper Output => _output ?? throw new InvalidOperationException("Fixture not initialized yet");

    public AltinnAppCoreFixture() { }

    internal void Initialize()
    {
        if (_isInitialized)
            return;

        var output = Output;
        var timer = Stopwatch.StartNew();
        try
        {
            _projectDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "Altinn.App.Core");
            Assert.True(Directory.Exists(_projectDir));
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(Path.Combine(_projectDir, "Altinn.App.Core.csproj"));
            _workspace = analyzer.GetWorkspace();
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.AddDocument));
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.RemoveDocument));
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.ChangeDocument));
            var solution = _workspace.CurrentSolution;
            _project = solution.Projects.Single(p => p.Name == "Altinn.App.Core");
            _isInitialized = true;

            timer.Stop();
            output.WriteLine($"Initialized Altinn.App.Core fixture - took {timer.Elapsed.TotalSeconds:0.000}s");
        }
        catch (Exception ex)
        {
            timer.Stop();
            output.WriteLine(
                $"Error initializing Altinn.App.Core fixture (took {timer.Elapsed.TotalSeconds:0.000}s): {ex}"
            );
            throw;
        }
    }

    public IDisposable WithCode(string code)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized");

        var modification = new ProjectModification(this);

        var doc = _project.AddDocument("Code.cs", SourceText.From(code, Encoding.UTF8));
        _project = doc.Project;
        Assert.True(_workspace.TryApplyChanges(_project.Solution));

        return modification;
    }

    public async Task<(CompilationWithAnalyzers Compilation, IReadOnlyList<Diagnostic>)> GetCompilation(
        DiagnosticAnalyzer analyzer,
        CancellationToken cancellationToken
    )
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized");

        var compilation = await _project.GetCompilationAsync(cancellationToken);
        Assert.NotNull(compilation);
        var globalOptions = new Dictionary<string, string>()
        {
            ["build_property.projectdir"] = _projectDir,
        }.ToImmutableDictionary();

        var analyzerOptions = new AnalyzerOptions(
            [],
            new TestOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                new TestAnalyzerConfigOptions(globalOptions)
            )
        );

        var options = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            static (ex, analyzer, diagnostic) => Assert.Fail($"Analyzer exception due to {diagnostic.Id}: {ex}"),
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: true
        );

        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer], options);

        Assert.NotNull(compilationWithAnalyzers);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);

        return (
            compilationWithAnalyzers,
            diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition).ToArray()
        );
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }

    /// <summary>
    /// During tests of analyzers we typically want to modify code to introduce some error that cause diagnostics to
    /// be emitted. When a test is done, it is important to revert the code to its original state.
    /// The Roslyn workspace/solution/project models are immutable, so to achieve
    /// this we can just capture the original variables from the fixture and put them back when
    /// the modification this class represents is disposed.
    ///
    /// Usage:
    ///   * Create a "With..." method in the fixture representing some modification to the project
    ///   * Before making modifications, new up this class to capture the original state
    ///   * In the "With..." method, make the modifications
    ///   * In the "With..." method, return the instance of this class that now contains the original state
    ///   * The caller is responsible for disposing the instance of this class, which reverts the fixture back to its original state
    /// </summary>
    private sealed record ProjectModification : IDisposable
    {
        private readonly AltinnAppCoreFixture _fixture;
        private readonly AdhocWorkspace _workspace;
        private readonly Project _project;
        private readonly Action? _action;

        internal ProjectModification(AltinnAppCoreFixture fixture, Action? action = null)
        {
            if (!fixture._isInitialized)
                throw new InvalidOperationException("Fixture not initialized");

            _fixture = fixture;
            _workspace = fixture._workspace;
            _project = fixture._project;
            _action = action;
        }

        public void Dispose()
        {
            _fixture._workspace = _workspace;
            _fixture._project = _project;
            _action?.Invoke();
        }
    }
}
