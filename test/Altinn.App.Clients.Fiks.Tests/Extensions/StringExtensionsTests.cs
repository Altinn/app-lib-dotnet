using Altinn.App.Clients.Fiks.Extensions;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using Xunit.Sdk;

namespace Altinn.App.Clients.Fiks.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void DeserializeXml_ValidXml_ReturnsObject()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <arkivmelding xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns="https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivmelding/opprett/v1">
              <system>record-system-id</system>
              <regel>record-rule-id</regel>
              <antallFiler>2</antallFiler>
            </arkivmelding>
            """;
        var expected = new Arkivmelding
        {
            System = "record-system-id",
            Regel = "record-rule-id",
            AntallFiler = 2,
        };

        // Act
        var result = xml.DeserializeXml<Arkivmelding>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.System, expected.System);
        Assert.Equal(result.Regel, expected.Regel);
        Assert.Equal(result.AntallFiler, expected.AntallFiler);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DeserializeXml_NullOrEmptyInput_ReturnsNull(string? input)
    {
        var result = input!.DeserializeXml<Arkivmelding>();
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeXml_InvalidXml_ThrowsException()
    {
        // Arrange
        const string xml = "This isn't XML";

        // Act
        var ex = Record.Exception(() => xml.DeserializeXml<Arkivmelding>());

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
    }
}
