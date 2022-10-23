using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Altinn.App.Core.Expressions;
using Altinn.App.Core.Models.Validation;
using FluentAssertions;
using Xunit;

namespace Altinn.App.Core.Tests.LayoutExpressions.FullTests.Test2;

public class RunTest2
{
    [Fact]
    public async Task ValidateDataModel()
    {
        var state = await LayoutTestUtils.GetLayoutModelTools(new DataModel(), "Test2");
        var errors = state.GetModelErrors();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveWholeGroup()
    {
        var state = await LayoutTestUtils.GetLayoutModelTools(
            new DataModel()
            {
                Some = new()
                {
                    NotRepeating = "hideGroup",
                    Data = new()
                    {
                       new() { Binding = "binding" },
                       new() { Binding2 = 2 },
                    }
                }
            },
            "Test2");
        var hidden = LayoutEvaluator.GetHiddenFieldsForRemoval(state);

        hidden.Should().BeEquivalentTo(new List<string> { "some.data[0].binding2", "some.data[1].binding2" });
    }

    [Fact]
    public async Task RemoveSingleRow()
    {
        var state = await LayoutTestUtils.GetLayoutModelTools(
            new DataModel()
            {
                Some = new()
                {
                    NotRepeating = "showGroup",
                    Data = new()
                    {
                       new() { Binding = "binding" },
                       new() { Binding2 = 2, Binding3 = "hidden" },
                    }
                }
            },
            "Test2");
        var hidden = LayoutEvaluator.GetHiddenFieldsForRemoval(state);

        hidden.Should().BeEquivalentTo(new List<string> { "some.data[1].binding2" });
    }
}

public class DataModel
{
    [JsonPropertyName("some")]
    public Some Some { get; set; }
}

public class Some
{
    [JsonPropertyName("notRepeating")]
    public string NotRepeating { get; set; }

    [JsonPropertyName("data")]
    public List<Data> Data { get; set; }
}

public class Data
{
    [JsonPropertyName("binding")]
    public string Binding { get; set; }

    [JsonPropertyName("binding2")]
    public int Binding2 { get; set; }

    [JsonPropertyName("binding3")]
    public string Binding3 { get; set; }
}