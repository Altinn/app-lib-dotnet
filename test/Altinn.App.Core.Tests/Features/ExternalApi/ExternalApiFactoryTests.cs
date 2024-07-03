using Altinn.App.Core.Features.ExternalApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Features.ExternalApi;

public class ExternalApiFactoryTests
{
    private readonly Mock<ILogger<ExternalApiFactory>> _loggerMock;
    private readonly Mock<IExternalApiClient> _externalApiClientMock;

    public ExternalApiFactoryTests()
    {
        _loggerMock = new Mock<ILogger<ExternalApiFactory>>();
        _externalApiClientMock = new Mock<IExternalApiClient>();
    }

    [Fact]
    public void GetExternalApiClient_UnknownApiId_ShouldThrowException()
    {
        // Arrange
        var factory = new ExternalApiFactory(_loggerMock.Object, []);

        // Act
        System.Action action = () => factory.GetExternalApiClient("unknown");

        // Assert
        action.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void GetExternalApiClient_ExistingApiId_ShouldReturnClient()
    {
        // Arrange
        _externalApiClientMock.SetupGet(x => x.Id).Returns("api1");
        var factory = new ExternalApiFactory(_loggerMock.Object, [_externalApiClientMock.Object]);

        // Act
        var externalApiClient = factory.GetExternalApiClient("api1");

        // Assert
        externalApiClient.Should().Be(_externalApiClientMock.Object);
        externalApiClient.Id.Should().Be("api1");
    }
}
