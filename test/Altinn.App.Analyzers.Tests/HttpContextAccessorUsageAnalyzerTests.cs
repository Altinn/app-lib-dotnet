using Altinn.App.Analyzers.Tests.Fixtures;
using Xunit.Abstractions;

namespace Altinn.App.Analyzers.Tests;

[Collection(nameof(AltinnTestAppCollection))]
public class HttpContextAccessorUsageAnalyzerTests
{
    private readonly AltinnTestAppFixture _fixture;

    public HttpContextAccessorUsageAnalyzerTests(AltinnTestAppFixture fixture, ITestOutputHelper output)
    {
        fixture.SetTestOutputHelper(output);
        fixture.Initialize();
        _fixture = fixture;
    }

    [Fact]
    public async Task Builds_OK_By_Default()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var cancellationToken = cts.Token;

        var analyzer = new HttpContextAccessorUsageAnalyzer();

        var (compilation, diagnostics) = await _fixture.GetCompilation(
            analyzer,
            includeAdditionalFiles: false,
            cancellationToken
        );

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Emits_Diagnostic()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var cancellationToken = cts.Token;

        using var modification = _fixture.WithInvalidHttpContextAccessorUse();
        var analyzer = new HttpContextAccessorUsageAnalyzer();

        var (compilation, diagnostics) = await _fixture.GetCompilation(
            analyzer,
            includeAdditionalFiles: false,
            cancellationToken
        );

        Assert.Contains(diagnostics, d => Diagnostics.CodeSmells.HttpContextAccessorUsage.Id == d.Id);
        await Verify(diagnostics);
    }
}
