using System.Net;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Internal.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Altinn.App.Core.Tests.Features.Options.Altinn3LibraryProvider;

public class Altinn3LibraryOptionsProviderTests
{
    private const string ClientName = "Altinn3LibraryClient";
    private const string OptionId = "SomeId";
    private const string Org = "ttd";
    private const string CodeListId = "SomeCodeListId";
    private const string Version = "1";

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_RequestingEnThenNb_ShouldReturnNbOptionsOnSecondCall()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        SetupHttpClientFactory(Altinn3LibraryOptionsProviderTestData.GetNbEnResponseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        await optionsProvider.GetAppOptionsAsync(LanguageConst.En, new Dictionary<string, string>());
        var result = await optionsProvider.GetAppOptionsAsync(LanguageConst.Nb, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("tekst", option.Label);
        Assert.Equal("Dette er en tekst", option.Description);
        Assert.Equal("Velg dette valget for å få en tekst", option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_LanguageCollectionsIsEmpty_ShouldReturnOptionsWithOnlyValueAndTags()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        var responseMessage = () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "codes": [
                        {
                          "value": "value1",
                          "label": {},
                          "description": {},
                          "helpText": {},
                          "tags": [
                            "test-data"
                          ]
                        }
                      ],
                      "version": "ttd/code_lists/someNewCodeList/1.json",
                      "source": {
                        "name": "test-data-files"
                      },
                      "tagNames": [
                        "test-data-category"
                      ]
                    }
                    """
                ),
            };

        SetupHttpClientFactory(responseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(null, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Empty(option.Label);
        Assert.NotNull(option.Description);
        Assert.Empty(option.Description);
        Assert.NotNull(option.HelpText);
        Assert.Empty(option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_LanguageCollectionsIsNull_ShouldReturnOptionsWithOnlyValueAndTags()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        var responseMessage = () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "codes": [
                        {
                          "value": "value1",
                          "label": null,
                          "description": null,
                          "helpText": null,
                          "tags": [
                            "test-data"
                          ]
                        }
                      ],
                      "version": "ttd/code_lists/someNewCodeList/1.json",
                      "source": {
                        "name": "test-data-files"
                      },
                      "tagNames": [
                        "test-data-category"
                      ]
                    }
                    """
                ),
            };

        SetupHttpClientFactory(responseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(null, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Null(option.Label);
        Assert.Null(option.Description);
        Assert.Null(option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_NoLanguageProvided_ShouldSortAndUseFirstLanguageInDictionaryWhenNeitherNbNorEnExists()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        var responseMessage = () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "codes": [
                        {
                          "value": "value1",
                          "label": {
                            "de": "text",
                            "se": "text"
                          },
                          "description": {
                            "de": "Das ist ein Text",
                            "se": "Det här är en text"
                          },
                          "helpText": {
                            "se": "Välj det här alternativet för att få ett text",
                            "de": "Wählen Sie diese Option, um eine Text zu erhalten"
                          },
                          "tags": [
                            "test-data"
                          ]
                        }
                      ],
                      "version": "ttd/code_lists/someNewCodeList/1.json",
                      "source": {
                        "name": "test-data-files"
                      },
                      "tagNames": [
                        "test-data-category"
                      ]
                    }
                    """
                ),
            };

        SetupHttpClientFactory(responseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(null, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("text", option.Label);
        Assert.Equal("Das ist ein Text", option.Description);
        Assert.Equal("Wählen Sie diese Option, um eine Text zu erhalten", option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_NoLanguageProvided_ShouldDefaultToEnWhenNbIsNotPresentInResponseButEnIs()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        var responseMessage = () =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "codes": [
                        {
                          "value": "value1",
                          "label": {
                            "de": "text",
                            "en": "text"
                          },
                          "description": {
                            "de": "Das ist ein Text",
                            "en": "This is a text"
                          },
                          "helpText": {
                            "en": "Choose this option to get a text",
                            "de": "Wählen Sie diese Option, um eine Text zu erhalten"
                          },
                          "tags": [
                            "test-data"
                          ]
                        }
                      ],
                      "version": "ttd/code_lists/someNewCodeList/1.json",
                      "source": {
                        "name": "test-data-files"
                      },
                      "tagNames": [
                        "test-data-category"
                      ]
                    }
                    """
                ),
            };

        SetupHttpClientFactory(responseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(null, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("text", option.Label);
        Assert.Equal("This is a text", option.Description);
        Assert.Equal("Choose this option to get a text", option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_NoLanguageProvided_ShouldDefaultToNbWhenNbIsPresentInResponse()
    {
        // Arrange
        await using var fixture = Fixture.Create();

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;

        SetupHttpClientFactory(Altinn3LibraryOptionsProviderTestData.GetNbEnResponseMessage, mockHttpClientFactory);

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(null, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("tekst", option.Label);
        Assert.Equal("Dette er en tekst", option.Description);
        Assert.Equal("Velg dette valget for å få en tekst", option.HelpText);
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_TwoCallsRequestingTheSameHybridCacheKey_ShouldCallMessageHandlerOnce()
    {
        // Arrange
        await using var fixture = Fixture.Create();
        const string uri = $"{Org}/code_lists/{CodeListId}/{Version}.json";

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;
        var mockHttpMessageHandler = SetupHttpClientFactory(
            Altinn3LibraryOptionsProviderTestData.GetNbEnResponseMessage,
            mockHttpClientFactory
        );

        var platformSettings = fixture.App.Services.GetService<IOptions<PlatformSettings>>()?.Value!;

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        await optionsProvider.GetAppOptionsAsync(LanguageConst.Nb, new Dictionary<string, string>());
        await optionsProvider.GetAppOptionsAsync(LanguageConst.Nb, new Dictionary<string, string>());

        // Assert
        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Get
                    && m.RequestUri != null
                    && m.RequestUri.ToString() == platformSettings.Altinn3LibraryApiEndpoint + uri
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task Altinn3LibraryOptionsProvider_CallsGetAppOptionsAsyncOnce_ShouldReturnsOptions()
    {
        // Arrange
        await using var fixture = Fixture.Create();
        const string uri = $"{Org}/code_lists/{CodeListId}/{Version}.json";

        var mockHttpClientFactory = fixture.HttpClientFactoryMock;
        var mockHttpMessageHandler = SetupHttpClientFactory(
            Altinn3LibraryOptionsProviderTestData.GetNbEnResponseMessage,
            mockHttpClientFactory
        );

        var platformSettings = fixture.App.Services.GetService<IOptions<PlatformSettings>>()?.Value!;

        // Act
        var optionsProvider = fixture.GetOptionsProvider(OptionId);
        var result = await optionsProvider.GetAppOptionsAsync(LanguageConst.Nb, new Dictionary<string, string>());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("value1", option.Value);
        Assert.Equal("tekst", option.Label);
        Assert.Equal("Dette er en tekst", option.Description);
        Assert.Equal("Velg dette valget for å få en tekst", option.HelpText);
        var versionParam = result.Parameters.Single(p => p.Key == "version");
        Assert.Equal("ttd/code_lists/someNewCodeList/1.json", versionParam.Value);
        var sourceParam = result.Parameters.Single(p => p.Key == "source");
        Assert.Equal("test-data-files", sourceParam.Value);

        mockHttpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(m =>
                    m.Method == HttpMethod.Get
                    && m.RequestUri != null
                    && m.RequestUri.ToString() == platformSettings.Altinn3LibraryApiEndpoint + uri
                ),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    private sealed record Fixture(WebApplication App) : IAsyncDisposable
    {
        public Mock<IHttpClientFactory> HttpClientFactoryMock =>
            Mock.Get(App.Services.GetRequiredService<IHttpClientFactory>());

        public IAppOptionsProvider GetOptionsProvider(string id) =>
            App.Services.GetRequiredService<IEnumerable<IAppOptionsProvider>>().Single(p => p.Id == id);

        public static Fixture Create()
        {
            var mockHttpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

            var app = AppBuilder.Build(registerCustomAppServices: services =>
            {
                services.AddSingleton(mockHttpClientFactory.Object);
                services.AddAltinn3CodeList(optionId: OptionId, org: Org, codeListId: CodeListId, version: Version);
            });

            return new Fixture(app);
        }

        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    private static Mock<HttpMessageHandler> SetupHttpClientFactory(
        Func<HttpResponseMessage> responseMessage,
        Mock<IHttpClientFactory> mockHttpClientFactory
    )
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => responseMessage());

        mockHttpClientFactory
            .Setup(f => f.CreateClient(ClientName))
            .Returns(() =>
            {
                return new HttpClient(mockHttpMessageHandler.Object);
            });

        return mockHttpMessageHandler;
    }
}
