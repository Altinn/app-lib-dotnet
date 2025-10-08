using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Tests.Extensions;

public class OrganizationNumberExtensionsTest
{
    [Fact]
    public void ToUrnFormattedString_WithValidOrganisationNumber_ReturnsCorrectUrn()
    {
        // Arrange
        var organisationNumber = IdentificationNumberProvider.OrganizationNumbers.GetValidNumber(1);

        // Act
        var result = organisationNumber.ToUrnFormattedString();

        // Assert
        Assert.Equal($"{AltinnUrns.OrganizationNumber}:{organisationNumber}", result);
    }

    [Fact]
    public void ToUrnFormattedString_WithNullOrganisationNumber_ReturnsNull()
    {
        // Arrange
        OrganizationNumber? organisationNumber = null;

        // Act
        var result = organisationNumber.ToUrnFormattedString();

        // Assert
        Assert.Null(result);
    }
}
