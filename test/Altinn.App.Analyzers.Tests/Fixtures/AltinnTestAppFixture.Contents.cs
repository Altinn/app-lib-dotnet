namespace Altinn.App.Analyzers.Tests.Fixtures;

internal sealed record DocumentSelector
{
    public DocumentSelector(params string[] path)
    {
        FilePath = Path.Combine([Directory.GetCurrentDirectory(), "App", .. path]);
    }

    public string FilePath { get; }

    public override string ToString() => FilePath;
}

partial class AltinnTestAppFixture
{
    static class Content
    {
        public static readonly DocumentSelector ModelClass = new DocumentSelector("models", "model.cs");
    }
}
