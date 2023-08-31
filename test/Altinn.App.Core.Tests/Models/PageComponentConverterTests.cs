#nullable enable

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
            if (testCase.ExpectedHierarchy is not null)
            {
                var page = JsonSerializer.Deserialize<PageComponent>(testCase.Layout)!;
                CompareChildren(testCase.ExpectedHierarchy, page.Children.ToArray());
            }
        }
        else
        {
            exception.Should().NotBeNull();
        }
    }

    private void ComparePageComponent(ExpectedHierarchyModel expected, BaseComponent component)
    {
        component.Id.Should().Be(expected.Id);

        if (expected.Children is null)
        {
            (component is GroupComponent).Should().BeFalse();
        }
        else
        {
            (component is GroupComponent).Should().BeTrue();
            CompareChildren(expected.Children, ((GroupComponent)component).Children.ToArray());
        }

    }

    private void CompareChildren(ExpectedHierarchyModel[] expectedChildren, BaseComponent[] children)
    {
        children.Length.Should().Be(expectedChildren.Length);

        for (int i = 0; i < expectedChildren.Length; i++)
        {
            ComparePageComponent(expectedChildren[i], children[i]);
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

    public ExpectedHierarchyModel[]? ExpectedHierarchy { get; set; }

}

public class ExpectedHierarchyModel
{
    public string Id { get; set; } = string.Empty;

    public ExpectedHierarchyModel[]? Children { get; set; }
}
