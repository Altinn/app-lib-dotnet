using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.SourceGenerator.Tests;

public class AdditionalTextImplementation : AdditionalText
{
    private readonly string? _text;

    public AdditionalTextImplementation(string? text, string filePath)
    {
        _text = text;
        Path = filePath;
    }

    public override SourceText? GetText(CancellationToken cancellationToken = new CancellationToken())
    {
        return _text != null ? new StringSourceText(_text) : null;
    }

    public override string Path { get; }

    private class StringSourceText : SourceText
    {
        private readonly string _text;

        public StringSourceText(string text)
        {
            _text = text;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _text.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override Encoding Encoding => Encoding.Unicode;
        public override int Length => _text.Length;

        public override char this[int position] => _text[position];
    }
}
