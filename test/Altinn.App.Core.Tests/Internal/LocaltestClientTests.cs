using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Altinn.App.Core.Internal.LocaltestClient;

namespace Altinn.App.Core.Tests.Internal;

public class LocaltestclientTests
{
    private sealed record Fixture(WebApplication App) : IAsyncDisposable
    {
        internal const string ApiPath = "/Home/Localtest/Version";

        public Mock<IHttpClientFactory> HttpClientFactoryMock =>
            Mock.Get(App.Services.GetRequiredService<IHttpClientFactory>());

        public WireMockServer Server => App.Services.GetRequiredService<WireMockServer>();

        public FakeTimeProvider TimeProvider => App.Services.GetRequiredService<FakeTimeProvider>();

        public static Fixture Create()
        {
            var server = WireMockServer.Start();

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var mockHttpClient = new Mock<HttpClient>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => server.CreateClient());

            var app = Api.Tests.TestUtils.AppBuilder.Build(registerCustomAppServices: services =>
            {
                services.AddSingleton(_ => server);

                services.Configure<GeneralSettings>(settings =>
                {
                    settings.LocaltestUrl = server.Url ?? throw new Exception("Missing server URL");
                });

                var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero));
                services.AddSingleton<TimeProvider>(fakeTimeProvider);
                services.AddSingleton(fakeTimeProvider);

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
    public async Task Test_Recent_Version()
    {
        await using var fixture = Fixture.Create();

        var expectedVersion = 1;

        var server = fixture.Server;
        server
            .Given(Request.Create().WithPath(Fixture.ApiPath).UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody($"{expectedVersion}")
            );

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);

        var result = await client.FirstResult;
        Assert.NotNull(result);
        var ok = Assert.IsType<VersionResult.Ok>(result);
        Assert.Equal(expectedVersion, ok.Version);

        var reqs = server.FindLogEntries(Request.Create().WithPath(Fixture.ApiPath).UsingGet());
        Assert.Single(reqs);

        Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);
    }

    [Fact]
    public async Task Test_Old_Version()
    {
        await using var fixture = Fixture.Create();

        var expectedVersion = 0;

        var server = fixture.Server;
        server
            .Given(Request.Create().WithPath(Fixture.ApiPath).UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody($"{expectedVersion}")
            );

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);

        var result = await client.FirstResult;
        Assert.NotNull(result);
        var ok = Assert.IsType<VersionResult.Ok>(result);
        Assert.Equal(expectedVersion, ok.Version);

        var reqs = server.FindLogEntries(Request.Create().WithPath(Fixture.ApiPath).UsingGet());
        Assert.Single(reqs);

        Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
    }

    [Fact]
    public async Task Test_Invalid_Version()
    {
        await using var fixture = Fixture.Create();

        var server = fixture.Server;
        server
            .Given(Request.Create().WithPath(Fixture.ApiPath).UsingGet())
            .RespondWith(
                Response.Create().WithStatusCode(200).WithHeader("Content-Type", "text/plain").WithBody("blah")
            );

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);

        var result = await client.FirstResult;
        Assert.NotNull(result);
        Assert.IsType<VersionResult.InvalidVersionResponse>(result);
    }

    [Fact]
    public async Task Test_Timeout()
    {
        await using var fixture = Fixture.Create();

        var expectedVersion = 1;
        var delay = TimeSpan.FromSeconds(6);

        var server = fixture.Server;
        server
            .Given(Request.Create().WithPath(Fixture.ApiPath).UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody($"{expectedVersion}")
                    .WithDelay(delay)
            );

        var client = fixture.App.Services.GetRequiredService<LocaltestClient>();
        var lifetime = fixture.App.Services.GetRequiredService<IHostApplicationLifetime>();
        await client.StartAsync(lifetime.ApplicationStopping);
        await Task.Delay(10);
        fixture.TimeProvider.Advance(delay);

        var result = await client.FirstResult;
        Assert.NotNull(result);
        Assert.IsType<VersionResult.Timeout>(result);
    }
}
