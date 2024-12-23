using System.Net;
using Altinn.App.Core.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Altinn.App.Core.Tests.Internal;

public class LocaltestclientTests
{
    private sealed record Fixture(WebApplication App) : IAsyncDisposable
    {
        public Mock<IHttpClientFactory> HttpClientFactoryMock =>
            Mock.Get(App.Services.GetRequiredService<IHttpClientFactory>());

        public Mock<HttpClient> HttpClientMock => Mock.Get(App.Services.GetRequiredService<HttpClient>());

        public static Fixture Create(bool realTest = false)
        {
            Mock<IHttpClientFactory>? mockHttpClientFactory = null;
            if (!realTest)
            {
                mockHttpClientFactory = new Mock<IHttpClientFactory>();
                var mockHttpClient = new Mock<HttpClient>();
                mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
            }

            var app = Api.Tests.TestUtils.AppBuilder.Build(registerCustomAppServices: services =>
            {
                if (!realTest)
                {
                    var fakeTimeProvider = new FakeTimeProvider(
                        new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero)
                    );
                    services.AddSingleton<TimeProvider>(fakeTimeProvider);
                    services.AddSingleton(fakeTimeProvider);
                }

                if (mockHttpClientFactory is not null)
                    services.AddSingleton(mockHttpClientFactory.Object);
            });

            return new Fixture(app);
        }

        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    [Fact]
    public async Task Test_Init()
    {
        await using var fixture = Fixture.Create();

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Test_Ok()
    {
        await using var fixture = Fixture.Create();

        var httpClient = fixture.HttpClientMock;
        httpClient
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"version\": 1}") }
            );

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task Test_Real() // Runs against localtest, add skip to avoid running this test on main
    {
        await using var fixture = Fixture.Create(realTest: true);

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);

        var result = await client.FirstResult;
        Assert.NotNull(result);
    }
}
