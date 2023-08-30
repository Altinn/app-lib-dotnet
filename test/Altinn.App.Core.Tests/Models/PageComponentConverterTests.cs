using System.Reflection;
using System.Text.Json;
using Altinn.App.Core.Models.Layout.Components;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace Altinn.App.Core.Tests.Models;

public class PageComponentConverterTests
{
    [Theory]
    [PageComponentConverterTest]
    public void RunPageComponentConverterTest(PageComponentConverterTestModel testCase)
    {
        var exception = Record.Exception(() => JsonSerializer.Deserialize<PageComponent>(testCase.Layout));

        if (testCase.Valid)
        {
            exception.Should().BeNull();
        }
        else
        {
            exception.Should().NotBeNull();
        }
    }
}

public class PageComponentConverterTestAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var files = Directory.GetFiles(Path.Join("Models", "page-component-converter-tests"));

        foreach (var file in files)
        {
            var data = File.ReadAllText(file);
            var testCase = JsonSerializer.Deserialize<PageComponentConverterTestModel>(data, new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
            yield return new object[] { testCase };
        }
    }
}

public class PageComponentConverterTestModel
{
    public bool Valid { get; set; }

    public JsonElement Layout { get; set; }
}
