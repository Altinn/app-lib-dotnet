using Altinn.App.Api.Extensions;
using Altinn.App.Api.Tests.TestUtils;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Delegates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Api.Tests.Maskinporten;

public class MaskinportenClientIntegrationTests
{
    [Fact]
    public void ConfigureAppWebHost_AddsMaskinportenService()
    {
        var app = AppBuilder.Build();
        app.Services.GetServices<IMaskinportenClient>().Count().Should().Be(1);
    }

    [Fact]
    public void ConfigureMaskinportenClient_AddsMaskinportenConfiguration()
    {
        // TODO: Test that ServiceCollectionExtensions.ConfigureMaskinportenClient adds a configuration
        Assert.Fail();
    }

    [Fact]
    public void ConfigureMaskinportenClient_OverridesOtherMaskinportenConfigurations()
    {
        // TODO: Test that ServiceCollectionExtensions.ConfigureMaskinportenClient overrides any other configurations added
        Assert.Fail();
    }

    [Fact]
    public void UseMaskinportenAuthorization_AddsHandler()
    {
        // Arrange
        var scopes = new[] { "scope1", "scope2" };
        var services = new ServiceCollection();
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider
            .Setup(provider => provider.GetService(typeof(IMaskinportenClient)))
            .Returns(new Mock<IMaskinportenClient>().Object);
        mockProvider
            .Setup(provider => provider.GetService(typeof(MaskinportenDelegatingHandler)))
            .Returns(new MaskinportenDelegatingHandler(scopes, mockProvider.Object));

        var mockBuilder = new Mock<IHttpClientBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        mockBuilder.Object.UseMaskinportenAuthorization(scopes);

        // Assert
        mockProvider.Verify(provider => provider.GetService(typeof(IMaskinportenClient)), Times.Once);
        services.Should().ContainSingle(s => s.ServiceType == typeof(IConfigureOptions<HttpClientFactoryOptions>));
    }
}
