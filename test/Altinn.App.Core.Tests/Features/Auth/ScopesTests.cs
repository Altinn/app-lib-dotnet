namespace Altinn.App.Core.Tests.Features.Auth;

using Altinn.App.Core.Features.Auth;

public class ScopesTests
{
    [Theory]
    [InlineData("scope1", "scope1", true)]
    [InlineData("SCOPE1", "scope1", false)]
    [InlineData(" scope1", "scope1", true)]
    [InlineData(" scope1 ", "scope1", true)]
    [InlineData("scope1", "scope2", false)]
    [InlineData("scope1 scope2", "scope1", true)]
    [InlineData("scope1  scope2", "scope1", true)]
    [InlineData("scope1   scope2", "scope1", true)]
    [InlineData("scope1\tscope2", "scope1", true)]
    [InlineData("scope1\nscope2", "scope1", true)]
    [InlineData("scope1\r\nscope2", "scope1", true)]
    [InlineData("scope1\tscope2", "scope2", true)]
    [InlineData("scope1\nscope2", "scope2", true)]
    [InlineData("scope1\r\nscope2", "scope2", true)]
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
    public void HasScope_Returns(string? inputScopes, string scopeToCheck, bool expected)
    {
        var scopes = new Scopes(inputScopes);
        Assert.Equal(expected, scopes.HasScope(scopeToCheck));
    }

    [Theory]
    [InlineData("altinn:instances.write", "altinn:", true)]
    [InlineData("altinn:instances.write", "altinn:serviceowner", false)]
    [InlineData("altinn:serviceowner/instances.write", "altinn:serviceowner", true)]
    [InlineData("ALTINN:serviceowner/instances.write", "altinn:serviceowner", false)]
    [InlineData("test:altinn:serviceowner/instances.write", "altinn:serviceowner", false)]
    [InlineData("aaltinn:serviceowner/instances.write", "altinn:serviceowner", false)]
    [InlineData(null, "scope1", false)]
    [InlineData("", "scope1", false)]
    [InlineData("  ", "scope1", false)]
    public void HasScopePrefix_Returns(string? inputScopes, string prefixToCheck, bool expected)
    {
        var scopes = new Scopes(inputScopes);
        Assert.Equal(expected, scopes.HasScopePrefix(prefixToCheck));
    }
}
