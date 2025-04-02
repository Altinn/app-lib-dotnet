// using System.Collections.Immutable;
// using Altinn.App.Analyzers.Tests.Fixtures;
// using Microsoft.CodeAnalysis;
// using Xunit.Abstractions;

// namespace Altinn.App.Analyzers.Tests;

// [Collection(nameof(AltinnTestAppCollection))]
// public class MetadataAnalyzerTests
// {
//     private readonly AltinnTestAppFixture _fixture;

//     public MetadataAnalyzerTests(AltinnTestAppFixture fixture, ITestOutputHelper output)
//     {
//         fixture.SetTestOutputHelper(output);
//         fixture.Initialize();
//         _fixture = fixture;
//     }

//     [Fact]
//     public async Task Builds_OK_By_Default()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         Assert.Empty(diagnostics);
//     }

//     [Fact]
//     public async Task Builds_OK_By_Default_With_AdditionalFiles()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: true, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         Assert.Empty(diagnostics);
//     }

//     [Fact]
//     public async Task Correct_Diagnostic_On_Unhandled_Exception()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();
//         analyzer.OnCompilationBefore = () => throw new InjectedException("On compilation");

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         var diagnostic = Assert.Single(diagnostics);
//         Assert.NotNull(diagnostic);
//         Assert.Equal(Diagnostics.UnknownError.Id, diagnostic.Id);
//         await VerifyDiagnostics(diagnostics);
//     }

//     [Fact]
//     public async Task AppMetadata_Read_Failure_Diagnostic()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();
//         analyzer.OnApplicationMetadataReadBefore = () => throw new InjectedException("On appmetadata read");

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         var diagnostic = Assert.Single(diagnostics);
//         Assert.NotNull(diagnostic);
//         Assert.Equal(Diagnostics.ApplicationMetadata.FileNotReadable.Id, diagnostic.Id);
//         await VerifyDiagnostics(diagnostics);
//     }

//     [Fact]
//     public async Task AppMetadata_Deserialization_Failure_Diagnostic()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();
//         analyzer.OnApplicationMetadataDeserializationBefore = () =>
//             throw new InjectedException("On appmetadata deserialization");

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         var diagnostic = Assert.Single(diagnostics);
//         Assert.NotNull(diagnostic);
//         Assert.Equal(Diagnostics.ApplicationMetadata.ParsingFailure.Id, diagnostic.Id);
//         await VerifyDiagnostics(diagnostics);
//     }

//     [Fact]
//     public async Task ClassRef_Unresolved_Emits_Warning()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         using var modification = _fixture.WithRemovedModelClass();
//         var analyzer = new MetadataAnalyzer();

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);

//         var diagnostic = Assert.Single(diagnostics);
//         Assert.NotNull(diagnostic);
//         Assert.Equal(Diagnostics.ApplicationMetadata.DataTypeClassRefInvalid.Id, diagnostic.Id);
//         await VerifyDiagnostics(diagnostics);
//     }

//     [Fact]
//     public async Task ClassRef_Resolves_Ok()
//     {
//         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
//         var cancellationToken = cts.Token;

//         var analyzer = new MetadataAnalyzer();

//         var compilation = await _fixture.GetCompilation(analyzer, includeAdditionalFiles: false, cancellationToken);
//         var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync(cancellationToken);
//         Assert.Empty(diagnostics);
//     }

//     private async Task VerifyDiagnostics(ImmutableArray<Diagnostic> diagnostics)
//     {
//         await Verify(diagnostics)
//             .ScrubLinesWithReplace(l =>
//             {
//                 var index = l.IndexOf("at Altinn.App.Analyzers"); // Start of a stack trace
//                 return index == -1 ? l : l.Substring(0, index) + "STACKTRACE";
//             });
//     }
// }
