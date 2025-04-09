using Altinn.App.Core.Features.TokenProvider;
using Altinn.App.Core.Internal.Auth;
using FluentAssertions;
using Moq;

namespace Altinn.App.Core.Tests.Features.TokenProvider;

public class SpecificTokenProviderFeatureTests
{
    [Fact]
    public void SpecificTokenProviderStateContext_UseToken_SetsAndResetsTokenValue()
    {
        // Arrange
        var stateContext = new SpecificTokenProviderStateContext();
        var testToken = "test-token-value";

        // Initial state should be empty
        stateContext.Current.TokenValue.Should().BeEmpty();

        // Act - Using a token within a scope
        using (var scope = stateContext.UseToken(testToken))
        {
            // Assert - Token should be set within the scope
            stateContext.Current.TokenValue.Should().Be(testToken);

            // Act - Using a nested token scope
            var nestedToken = "nested-token-value";
            using (var nestedScope = stateContext.UseToken(nestedToken))
            {
                // Assert - Nested token should be active
                stateContext.Current.TokenValue.Should().Be(nestedToken);
            }

            // Assert - After nested scope is disposed, original token should be active again
            stateContext.Current.TokenValue.Should().Be(testToken);
        }

        // Assert - After scope is disposed, token should be reset to empty
        stateContext.Current.TokenValue.Should().BeEmpty();
    }

    [Fact]
    public void SpecificTokenProvider_GetToken_ReturnsContextTokenWhenAvailable()
    {
        // Arrange
        var defaultTokenValue = "default-token";
        var mockDefaultProvider = new Mock<IUserTokenProvider>();
        mockDefaultProvider.Setup(p => p.GetToken()).Returns(defaultTokenValue);

        var stateContext = new SpecificTokenProviderStateContext();

        var tokenProvider = new SpecificTokenProvider(mockDefaultProvider.Object, stateContext);

        // Act & Assert - Should use default provider when no context token is set
        tokenProvider.GetToken().Should().Be(defaultTokenValue);

        // Arrange - Set a specific token in the context
        var specificTokenValue = "specific-token";
        using (stateContext.UseToken(specificTokenValue))
        {
            // Act & Assert - Should use the token from context
            tokenProvider.GetToken().Should().Be(specificTokenValue);
        }

        // Act & Assert - Should use default provider again after scope is disposed
        tokenProvider.GetToken().Should().Be(defaultTokenValue);
    }
}
