namespace Altinn.App.Core.Tests.Features.Auth;

using Altinn.App.Core.Features.Auth;

public class ScopesTests
{
    [Theory]
    [InlineData("scope1", "scope1", true)]
    [InlineData("scope1", "scope2", false)]
    [InlineData("scope1 scope2", "scope1", true)]
    [InlineData("scope1  scope2", "scope1", true)]
    [InlineData("scope1   scope2", "scope1", true)]
    [InlineData("scope1\tscope2", "scope1", true)]
    [InlineData("scope1\nscope2", "scope1", true)]
    [InlineData("scope1 scope2", "scope2", true)]
    [InlineData("scope1 scope2", "scope3", false)]
    [InlineData("scope1  scope2", "scope3", false)]
    [InlineData("scope1   scope2", "scope3", false)]
    [InlineData("scope1\tscope2", "scope3", false)]
    [InlineData("scope1\nscope2", "scope3", false)]
    [InlineData("prefixscope1", "scope1", false)]
    [InlineData("scope1suffix", "scope1", false)]
    [InlineData("prefixscope1suffix", "scope1", false)]
    [InlineData(null, "scope1", false)]
    [InlineData("", "scope1", false)]
    [InlineData("  ", "scope1", false)]
    public void HasScope_Returns(string? scopes, string scopeToCheck, bool expected)
    {
        var s = new Scopes(scopes);
        Assert.Equal(expected, s.HasScope(scopeToCheck));
    }
}
