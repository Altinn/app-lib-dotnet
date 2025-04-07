using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.Analyzers.Tests.Fixtures;

internal sealed class TestAdditionalText(DocumentSelector selector) : AdditionalText
{
    public override string Path => selector.FilePath;

    public override SourceText? GetText(CancellationToken cancellationToken = default) =>
        SourceText.From(File.ReadAllText(selector.FilePath, Encoding.UTF8), Encoding.UTF8);
}
