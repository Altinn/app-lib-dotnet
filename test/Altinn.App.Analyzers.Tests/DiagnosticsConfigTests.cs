namespace Altinn.App.Analyzers.Tests;

public class DiagnosticsConfigTests
{
    [Fact]
    public void All_Diagnostics_Exist()
    {
        var diagnostics = Diagnostics.All;

        Assert.NotEmpty(Diagnostics.All);
        Assert.All(diagnostics, d => d.Id.StartsWith("ALTINNAPP", StringComparison.Ordinal));
    }
}
