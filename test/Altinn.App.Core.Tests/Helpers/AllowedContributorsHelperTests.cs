using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Helpers;

public class AllowedContributorsHelperTests
{
    [Theory]
    [InlineData(null, null, null, true)] // No allowed contributors, should be true
    [InlineData("org:altinn", "altinn", null, true)] // Matching org
    [InlineData("org:altinn", "Altinn", null, true)] // Matching org, case insensitive
    [InlineData("org:altinn", "notAltinn", null, false)] // Non-matching org
    [InlineData("orgno:12345678", null, 12345678, true)] // Matching orgNr
    [InlineData("orgno:12345678", null, 87654321, false)] // Non-matching orgNr
    [InlineData("orgno:12345678", null, null, false)] // orgNr is null
    [InlineData("org:altinn,orgno:12345678", "altinn", 12345678, true)] // Matching both
    [InlineData("org:altinn,orgno:12345678", "altinn", 87654321, true)] // Matching org only
    [InlineData("org:altinn,orgno:12345678", "notAltinn", 12345678, true)] // Matching orgNr only
    [InlineData("org:altinn,orgno:12345678", "notAltinn", 87654321, false)] // Non-matching both
    [InlineData("app:owned", null, null, false)] // App owned, no matching
    [InlineData("app:owned", "org:altinn", null, false)] // App owned, matching org
    [InlineData("app:owned", "org:altinn", 12345678, false)] // App owned, matching both
    [InlineData("app:owned", null, 12345678, false)] // App owned, matching orgNr
    public void IsValidContributor_ShouldReturnExpectedResult(
        string? allowedContributors,
        string? org,
        int? orgNr,
        bool expectedResult
    )
    {
        // Arrange
        var dataType = new DataType
        {
            AllowedContributers = allowedContributors?.Split(',')?.ToList() ?? new List<string>(),
        };

        // Act
        bool result = AllowedContributorsHelper.IsValidContributor(dataType, org, orgNr);

        // Assert
        result.Should().Be(expectedResult);
    }
}
