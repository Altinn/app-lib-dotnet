using Altinn.App.Api.Extensions;
using Altinn.App.Api.Tests.TestUtils;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Api.Tests.Maskinporten;

public class MaskinportenClientIntegrationTests
{
    [Fact]
    public void ConfigureAppWebHost_AddsMaskinportenService()
    {
        var app = AppBuilder.Build();
        app.Services.GetServices<IMaskinportenClient>().Should().HaveCount(1);
    }

    [Fact]
    public void ConfigureMaskinportenClient_OverridesDefaultMaskinportenConfiguration()
    {
        // Arrange
        var clientId = "the-client-id";
        var authority = "https://maskinporten.dev/";

        // Act
        var app = AppBuilder.Build(registerCustomAppServices: services =>
        {
            services.ConfigureMaskinportenClient(config =>
            {
                config.ClientId = clientId;
                config.Authority = authority;
                config.Key = new JsonWebKey();
            });
        });

        // Assert
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<MaskinportenSettings>>();
        Assert.NotNull(optionsMonitor);

        var settings = optionsMonitor.CurrentValue;
        Assert.NotNull(settings);

        settings.ClientId.Should().Be(clientId);
        settings.Authority.Should().Be(authority);
    }

    [Fact]
    public void ConfigureMaskinportenClient_AddsSingleConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var clientId = "the-client-id";
        var authority = "https://maskinporten.dev/";

        // Act
        services.ConfigureMaskinportenClient(config =>
        {
            config.ClientId = "this should be overwritten";
            config.Authority = "ditto";
            config.Key = new JsonWebKey();
        });
        services.ConfigureMaskinportenClient(config =>
        {
            config.ClientId = clientId;
            config.Authority = authority;
            config.Key = new JsonWebKey();
        });

        // Assert
        services.GetOptionsDescriptors<MaskinportenSettings>().Should().HaveCount(1);

        var serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MaskinportenSettings>>();
        Assert.NotNull(optionsMonitor);

        var settings = optionsMonitor.CurrentValue;
        Assert.NotNull(settings);

        settings.ClientId.Should().Be(clientId);
        settings.Authority.Should().Be(authority);
    }

    [Fact]
    public async Task UseMaskinportenAuthorization_AddsHandler()
    {
        // Arrange
        var scopes = new[] { "scope1", "scope2" };
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddSingleton<IMaskinportenClient, MaskinportenClient>();
        services.AddHttpClient<DummyHttpClient>().UseMaskinportenAuthorization(scopes);

        // Act
        await using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<DummyHttpClient>();

        // Assert
        Assert.NotNull(client);
        // TODO: inspect underlying handler? Verify that the handler is called somehow?
    }

    private sealed class DummyHttpClient
    {
        public DummyHttpClient(HttpClient client) { }
    }
}
