#nullable disable
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Models;

public class LanguageCodeTests
{
    static readonly string[] _validIso6391Codes = ["aa", "Bb", "CC", "zz"];

    static readonly string[] _invalidIso6391Codes = ["a.", " b", "abc", "😎🤓"];

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
            Action act = () => LanguageCode<ISO_639_1>.Parse(invalidCode);
            act.Should().Throw<FormatException>();
        }
    }

    [Fact]
    public void EqualityWorksAsExpected()
    {
        var langCode1A = LanguageCode<ISO_639_1>.Parse(_validIso6391Codes[0]);
        var langCode1B = LanguageCode<ISO_639_1>.Parse(_validIso6391Codes[0]);
        var langCode2 = LanguageCode<ISO_639_1>.Parse(_validIso6391Codes[2]);

        Assert.True(langCode1A == langCode1B);
        Assert.True(langCode1A != langCode2);
        Assert.False(langCode1A == langCode2);

        langCode1A.Should().Be(langCode1B);
        langCode1A.Should().NotBe(langCode2);
    }
}
