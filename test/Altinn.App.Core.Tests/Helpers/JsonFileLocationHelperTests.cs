using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Helpers;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Helpers;

public class JsonFileLocationHelperTests
{
    [Theory]
    [InlineData("a/4/c", "\"c\":")]
    [InlineData("a/4/c/", "\"value\"")]
    [InlineData("a/0", "1")]
    [InlineData("a/1/0", "45")]
    [InlineData("a/1/0/", "45")]
    [InlineData("a/1/0///", "45")]
    [InlineData("a/4/not-found", "{\"c\": \"value\"}")]
    [InlineData("a/2/array", "\"array\":")]
    [InlineData("a/2/array/0", "1")]
    [InlineData("a/2/array/", "[1,2,3]")]
    [InlineData("a/2/array//", "[1,2,3]")]
    public void TheoryTests(string jsonPath, string expectedValue)
    {
        var bytes = GetBytesFromJson(
            """
            {
              "test": "value",
              "a":[
                    1,
                    [45,{"b": "value"}],
                    {"array": [1,2,3]},
                    3,
                    {"c": "value"}
                ]
            }
            """
        );
        // Split the json path into segments
        var segments = jsonPath.Split("/");

        var range = JsonFileLocationHelper.GetByteRangeFromJsonPointerSegments(bytes, segments);
        var value = Encoding.UTF8.GetString(bytes[range]);
        value.Should().Be(expectedValue);
    }

    // Helper function to get JSON syntax highlighting in C# string
    private static byte[] GetBytesFromJson([StringSyntax("json")] string json)
    {
        return Encoding.UTF8.GetBytes(json);
    }
}
