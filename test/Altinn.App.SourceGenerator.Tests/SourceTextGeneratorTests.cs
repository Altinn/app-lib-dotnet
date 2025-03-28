using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using CSharpier;
using Xunit.Abstractions;

namespace Altinn.App.SourceGenerator.Tests;

public class SourceTextGeneratorTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task Generate()
    {
        var rootNode = GetRoot<Skjema>();

        var text = SourceTextGenerator.SourceTextGenerator.GenerateSourceText(rootNode, "internal");
        outputHelper.WriteLine(AddLineNumbers(text));
        var formattedText = await CodeFormatter.FormatAsync(
            text,
            new CodeFormatterOptions() { EndOfLine = EndOfLine.CRLF, Width = 120 }
        );
        Assert.Empty(formattedText.CompilationErrors);
        await Verify(text, extension: "cs").AutoVerify((v) => false);

        Assert.Equal(text, formattedText.Code);
    }

    [Fact]
    public async Task GenerateEmpty()
    {
        var rootNode = GetRoot<Empty>();

        var text = SourceTextGenerator.SourceTextGenerator.GenerateSourceText(rootNode, "internal");
        outputHelper.WriteLine(AddLineNumbers(text));
        var formattedText = await CodeFormatter.FormatAsync(
            text,
            new CodeFormatterOptions() { EndOfLine = EndOfLine.CRLF, Width = 120 }
        );
        Assert.Empty(formattedText.CompilationErrors);
        await Verify(text, extension: "cs").AutoVerify((v) => false);

        Assert.Equal(text, formattedText.Code);
    }

    private void RemoveAltinnRowIds() { }

    private string AddLineNumbers(string text)
    {
        var lines = text.Split('\n');
        var bytes = 0;
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            sb.Append($"{i + 1, 4} {$"({bytes})", -6}: {lines[i]}\n");
            bytes += lines[i].Length + 1;
        }
        return sb.ToString();
    }

    private ModelPathNode GetRoot<T>()
    {
        var children = ImmutableArray.Create(typeof(T).GetProperties().Select(GetFromType).ToArray());
        return new ModelPathNode("", "", typeof(T).FullName!, children);
    }

    private ModelPathNode GetFromType(PropertyInfo propertyInfo)
    {
        var propertyType = propertyInfo.PropertyType;
        var cSharpName = propertyInfo.Name;
        var jsonPath = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? cSharpName;

        string? listType = null;
        var typeString = propertyType.FullName!;
        var collectionInterface = propertyType.GetInterface("System.Collections.Generic.ICollection`1");

        if (collectionInterface != null)
        {
            var typeParam = collectionInterface.GetGenericArguments()[0];
            typeString = typeParam.FullName!;
            listType = propertyType.GetGenericTypeDefinition().FullName!.TrimEnd('`', '1');
            var children = GetChildren(typeParam);

            return new ModelPathNode(cSharpName, jsonPath, typeString, children, listType: listType);
        }
        else
        {
            var children = GetChildren(propertyType);

            return new ModelPathNode(cSharpName, jsonPath, typeString, children, listType: listType);
        }
    }

    private ImmutableArray<ModelPathNode> GetChildren(Type propertyType)
    {
        if (propertyType.Namespace?.StartsWith("System") == true)
        {
            return ImmutableArray<ModelPathNode>.Empty;
        }
        var properties = propertyType.GetProperties();
        var children = ImmutableArray.Create(properties.Select(GetFromType).ToArray());
        return children;
    }
}
