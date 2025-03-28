using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.Tests.Extensions;

public class InstanceExtensionsTests
{
    private static Instance GetInstance(params string[] dataTypes)
    {
        return new Instance { Data = dataTypes.Select(x => new DataElement { DataType = x }).ToList() };
    }

    [Fact]
    public void GetOptionalDataElements_ReturnsCorrectElements()
    {
        // Arrange
        var instance = GetInstance("type1", "TYPE1", "type2");

        // Act
        var result = instance.GetOptionalDataElements("Type1").ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("type1", result[0].DataType);
        Assert.Equal("TYPE1", result[1].DataType);
    }

    [Fact]
    public void GetOptionalDataElements_NoMatchingElements_ReturnsEmpty()
    {
        // Arrange
        var instance = GetInstance("type2");

        // Act
        var result = instance.GetOptionalDataElements("type1");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetRequiredDataElement_ReturnsCorrectElement()
    {
        // Arrange
        var instance = GetInstance("type1", "type2");

        // Act
        var result = instance.GetRequiredDataElement("TYPE1");

        // Assert
        Assert.Equal("type1", result.DataType);
    }

    [Fact]
    public void GetRequiredDataElement_NoMatchingElement_ThrowsException()
    {
        // Arrange
        var instance = GetInstance("type1");

        // Act
        var ex = Record.Exception(() => instance.GetRequiredDataElement("type2"));

        // Assert
        Assert.IsType<FiksArkivException>(ex);
    }
}
