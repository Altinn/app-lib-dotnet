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

public sealed partial class AltinnTestAppFixture : IDisposable
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

    public AltinnTestAppFixture() { }

    internal void Initialize()
    {
        if (_isInitialized)
            return;

        var output = Output;
        var timer = Stopwatch.StartNew();
        try
        {
            _projectDir = Path.Combine(Directory.GetCurrentDirectory(), "testapp", "App");
            Assert.True(Directory.Exists(_projectDir));
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(Path.Combine(_projectDir, "App.csproj"));
            _workspace = analyzer.GetWorkspace();
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.AddDocument));
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.RemoveDocument));
            Assert.True(_workspace.CanApplyChange(ApplyChangesKind.ChangeDocument));
            var solution = _workspace.CurrentSolution;
            _project = solution.Projects.Single(p => p.Name == "App");
            _isInitialized = true;

            timer.Stop();
            output.WriteLine($"Initialized Altinn test app fixture - took {timer.Elapsed.TotalSeconds:0.000}s");
        }
        catch (Exception ex)
        {
            timer.Stop();
            output.WriteLine(
                $"Error initializing Altinn test app fixture (took {timer.Elapsed.TotalSeconds:0.000}s): {ex}"
            );
            throw;
        }
    }

    public IDisposable WithRemovedModelClass()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized");

        var content = Content.ModelClass;

        var modification = new ProjectModification(this);

        var doc = _project.Documents.Single(d => d.FilePath == content.FilePath);
        _project = _project.RemoveDocument(doc.Id);
        Assert.True(_workspace.TryApplyChanges(_project.Solution));

        return modification;
    }

    public IDisposable WithInvalidHttpContextAccessorUse()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Fixture not initialized");

        var content = Content.InvalidHttpContextAccessorUse;

        var modification = new ProjectModification(this);

        var doc = _project.AddDocument(
            content.FilePath,
            SourceText.From(File.ReadAllText(content.FilePath, Encoding.UTF8), Encoding.UTF8)
        );
        _project = doc.Project;
        Assert.True(_workspace.TryApplyChanges(_project.Solution));

        return modification;
    }

    public async Task<(CompilationWithAnalyzers Compilation, IReadOnlyList<Diagnostic>)> GetCompilation(
        DiagnosticAnalyzer analyzer,
        bool includeAdditionalFiles,
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

        var additionalFiles = ImmutableArray.CreateRange<AdditionalText>(
            [new TestAdditionalText(Content.ApplicationMetadata), new TestAdditionalText(Content.LayoutSets)]
        );

        var analyzerOptions = new AnalyzerOptions(
            includeAdditionalFiles ? additionalFiles : [],
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
}
