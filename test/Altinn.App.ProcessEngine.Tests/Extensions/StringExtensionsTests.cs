using System.Globalization;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void FormatWith_HandlesInstanceInformationScenario()
    {
        // Arrange
        var instanceInfo = new InstanceInformation
        {
            Org = "the-org",
            App = "the-app",
            InstanceOwnerPartyId = 1234,
            InstanceGuid = Guid.Parse("013b0a97-19d9-464c-b5ce-7ca0f95bfce4"),
        };
        const string template =
            "http://the-host.com/{Org}/{APP}/instances/{instanceOwnerPartyId}/{instanceGuid}/the-endpoint";

        // Act
        var result = template.FormatWith(instanceInfo);

        // Assert
        Assert.Equal(
            "http://the-host.com/the-org/the-app/instances/1234/013b0a97-19d9-464c-b5ce-7ca0f95bfce4/the-endpoint",
            result
        );
    }

    [Fact]
    public void FormatWith_HandlesCustomScenario()
    {
        // Arrange
        const string template = "Test: {one}, {TWO}, {Three}, {four}";
        var customObject = new
        {
            One = 1,
            Two = "two",
            Three = 3.5,
            Four = "four",
        };

        // Act
        var result_en = template.FormatWith(customObject, new CultureInfo("en-US", false));
        var result_nb = template.FormatWith(customObject, new CultureInfo("nb-NO", false));
        var result_default = template.FormatWith(customObject);

        // Assert
        Assert.Equal("Test: 1, two, 3,5, four", result_nb);
        Assert.Equal("Test: 1, two, 3.5, four", result_en);
        Assert.Equal("Test: 1, two, 3.5, four", result_default);
    }
}
