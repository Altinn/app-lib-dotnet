#nullable disable
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Models;

public class LanguageCodeTests
{
    static readonly IEnumerable<string> _validIso6391Codes = ["aa", "Bb", "CC", "zz"];

    static readonly IEnumerable<string> _invalidIso6391Codes = ["a.", " b", "abc", "😎🤓"];

    [Fact]
    public void ValidIso6391CodesParseOk()
    {
        foreach (var validCode in _validIso6391Codes)
        {
            var langCode = LanguageCode<ISO_639_1>.Parse(validCode);
            langCode.Get().Should().Be(validCode.ToLowerInvariant());
        }
    }

    [Fact]
    public void InvalidIso6391CodesThrowException()
    {
        foreach (var invalidCode in _invalidIso6391Codes)
        {
            Action act = () => OrganisationNumber.Parse(invalidCode);
            act.Should().Throw<FormatException>();
        }
    }
}
