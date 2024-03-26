using System.Net;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.App.PlatformServices.Tests.Options.Altinn2Provider;
using Altinn.Common.AccessTokenClient.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Extensions;

public class HttpClientBuilderExtensionsTests
{
    private const string Org = "ttd";
    private const string App = "app";
    private static readonly ApplicationMetadata DefaultApplicationMetadata = new($"{Org}/{App}")
    {
        Org = Org,
    };

    private static readonly string PlatformToken = "**platform-token**";
    private const string InncommingUserToken = "**user-token**";

    private readonly Mock<IAppMetadata> _appMetadataMock = new(MockBehavior.Strict);
    private readonly Mock<IAccessTokenGenerator> _accessTokenGeneratorMock = new(MockBehavior.Strict);
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new(MockBehavior.Strict);
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly HeaderCaptureHttpMessageHandler _headerCaptureHttpMessageHandler = new();

    public HttpClientBuilderExtensionsTests()
    {
        _appMetadataMock
            .Setup(m => m.GetApplicationMetadata())
            .ReturnsAsync(DefaultApplicationMetadata);
        _accessTokenGeneratorMock
            .Setup(g => g.GenerateAccessToken(Org, App))
            .Returns(PlatformToken);
        _services.AddSingleton(_appMetadataMock.Object);
        _services.AddSingleton(_accessTokenGeneratorMock.Object);
        _services.AddSingleton(_httpContextAccessorMock.Object);

        // _services.Configure<>()
        _services.ConfigureHttpClientDefaults(opt =>
        {
            opt.ConfigurePrimaryHttpMessageHandler(sp => _headerCaptureHttpMessageHandler);
        });
        _services.AddHttpClient<TestHttpClient>();
    }

    [Fact]
    public async Task AddPlatformToken_AddsPlatformTokenMessageHandler()
    {
        // Arrange
        _services.AddHttpClient<TestHttpClient>()
            .AddPlatformToken();
        var serviceProvider = _services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<TestHttpClient>();

        // Act
        await client.GetAsync("http://example.com/testUrl");

        // Assert
        _headerCaptureHttpMessageHandler.Headers.Should()
            .ContainKey("PlatformAccessToken").WhoseValue.Should().Be(PlatformToken);
    }

    [Fact]
    public async Task AddPlatformToken_AddsPlatformTokenAndAuthToken()
    {
        // Arrange
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Headers)
            .Returns(new HeaderDictionary()
            {
                {"Authorization", $"Bearer {InncommingUserToken}"}
            });
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Cookies["AltinnStudioRuntime"])
            .Returns(null as string);
        _services.AddHttpClient<TestHttpClient>()
            .AddPlatformToken()
            .AddAuthToken();

        // Act
        var serviceProvider = _services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<TestHttpClient>();
        await client.GetAsync("http://example.com/testUrl");

        // Assert
        _headerCaptureHttpMessageHandler.Headers.Should()
            .ContainKey("PlatformAccessToken").WhoseValue.Should().Be(PlatformToken);
        _headerCaptureHttpMessageHandler.Headers.Should()
            .ContainKey("Authorization").WhoseValue.Should().Be($"Bearer {InncommingUserToken}");
    }

    [Fact]
    public async Task AddPlatformToken_AddsAuthTokenFromBearer()
    {
        // Arrange
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Headers)
            .Returns(new HeaderDictionary()
            {
                { "Authorization", $"Bearer {InncommingUserToken}" }
            });
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Cookies["AltinnStudioRuntime"])
            .Returns(null as string);
        _services.AddHttpClient<TestHttpClient>()
            .AddAuthToken();

        // Act
        var serviceProvider = _services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<TestHttpClient>();
        await client.GetAsync("http://example.com/testUrl");

        // Assert
        _headerCaptureHttpMessageHandler.Headers.Should()
            .ContainKey("Authorization").WhoseValue.Should().Be($"Bearer {InncommingUserToken}");
    }

    [Fact]
    public async Task AddPlatformToken_AddsAuthTokenFromCookie()
    {
        // Arrange
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Headers)
            .Returns(new HeaderDictionary());
        _httpContextAccessorMock.SetupGet(p => p.HttpContext!.Request.Cookies["AltinnStudioRuntime"])
            .Returns(InncommingUserToken);

        _services.AddHttpClient<TestHttpClient>()
            .AddAuthToken();

        // Act
        var serviceProvider = _services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<TestHttpClient>();
        await client.GetAsync("http://example.com/testUrl");

        // Assert
        _headerCaptureHttpMessageHandler.Headers.Should()
            .ContainKey("Authorization").WhoseValue.Should().Be($"Bearer {InncommingUserToken}");
    }

    private class TestHttpClient
    {
        private readonly HttpClient _httpClient;

        public TestHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return await _httpClient.GetAsync(requestUri);
        }
    }

    private class HeaderCaptureHttpMessageHandler : HttpMessageHandler
    {
        public Dictionary<string, string>? Headers { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Headers = request.Headers.ToDictionary(k => k.Key, v => v.Value.Single());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}