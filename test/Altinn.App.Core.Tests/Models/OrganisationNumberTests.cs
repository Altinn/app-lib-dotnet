#nullable disable
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Models;

public class OrganisationNumberTests
{
    static readonly IEnumerable<string> _validOrganisationNumbers =
    [
        "474103390",
        "593422461",
        "331660698",
        "904162426",
        "316620612",
        "452496593",
        "591955012",
        "343679238",
        "874408522",
        "857498941",
        "084209694",
        "545482657",
        "713789208",
        "149618953",
        "014888918",
        "184961733",
        "825076719",
        "544332597",
        "579390867",
        "930771813",
        "207154156",
        "601050765",
        "085483285"
    ];

    static readonly IEnumerable<string> _invalidOrganisationNumbers =
    [
        "474103392",
        "593422460",
        "331661698",
        "904172426",
        "316628612",
        "452496592",
        "591956012",
        "343679338",
        "874408520",
        "857498949",
        "084239694",
        "545487657",
        "623752180",
        "177442146",
        "262417258",
        "897200890",
        "509527177",
        "956866735",
        "760562895",
        "516103886",
        "192411646",
        "486551298",
        "370221387",
        "569288067",
        "322550165",
        "773771810",
        "862984904",
        "548575390",
        "183139014",
        "181318036",
        "843828242",
        "668910901",
        "123456789",
        "987654321",
        "12345",
        "08548328f"
    ];

    [Fact]
    public void ValidNumbersParseOk()
    {
        foreach (var validOrgNumber in _validOrganisationNumbers)
        {
            var orgNumber = OrganisationNumber.Parse(validOrgNumber);
            var orgNumberLocal = orgNumber.Get(OrganisationNumberFormat.Local);
            var orgNumberInternational = orgNumber.Get(OrganisationNumberFormat.International);

            orgNumberLocal.Should().Be(validOrgNumber);
            orgNumberInternational.Should().Be($"0192:{validOrgNumber}");
        }
    }

    [Fact]
    public void InvalidNumbersThrowException()
    {
        foreach (var invalidOrgNumber in _invalidOrganisationNumbers)
        {
            Action act = () => OrganisationNumber.Parse(invalidOrgNumber);
            act.Should().Throw<FormatException>();
        }
    }
}
