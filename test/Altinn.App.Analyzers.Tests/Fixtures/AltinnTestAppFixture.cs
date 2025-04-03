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

// This fixture is used to provide a test app Roslyn workspace for the analyzers to run on.
// The test app is a real blank Altinn app in the "testapp/" folder.
// Initializing the fixture is expensive, and can take anywhere between 5-20 seconds on my machine currently,
// so currently tests run in a "global collection" to avoid re-initializing the fixture for each test.
// It also gives us some flexibility in that we can make physical changes to project files.

[CollectionDefinition(nameof(AltinnTestAppCollection), DisableParallelization = true)]
public class AltinnTestAppCollection : ICollectionFixture<AltinnTestAppFixture> { }

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
            _projectDir = Path.Combine(Directory.GetCurrentDirectory(), "App");
            Assert.True(Directory.Exists(_projectDir));
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(Path.Combine(Directory.GetCurrentDirectory(), "App", "App.csproj"));
            _workspace = analyzer.GetWorkspace(addProjectReferences: true);
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

    public async Task<CompilationWithAnalyzers> GetCompilation(
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
        return compilationWithAnalyzers;
    }

    private sealed class TestAdditionalText(DocumentSelector selector) : AdditionalText
    {
        public override string Path => selector.FilePath;

        public override SourceText? GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(File.ReadAllText(selector.FilePath, Encoding.UTF8), Encoding.UTF8);
    }

    public void Dispose()
    {
        _workspace?.Dispose();
    }
}
