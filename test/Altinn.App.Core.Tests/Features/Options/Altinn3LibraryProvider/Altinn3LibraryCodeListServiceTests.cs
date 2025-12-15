using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Options.Altinn3LibraryCodeList;
using Altinn.App.Core.Internal.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Tests.Features.Options.Altinn3LibraryProvider;

public class Altinn3LibraryCodeListServiceTests
{
    private const string Org = "ttd";
    private const string CodeListId = "SomeCodeListId";
    private const string Version = "1";
    private const string ExpectedUri = $"{Org}/code_lists/{CodeListId}/{Version}.json";

    [Fact]
    public async Task GetCachedCodeListResponseAsync_TwoCallsRequestingDifferentHybridCacheKeys_ShouldCallMessageHandlerTwice()
    {
        // Arrange
        const string codeListIdTwo = "SomeOtherCodeListId";
        const string expectedUriTwo = $"{Org}/code_lists/{codeListIdTwo}/{Version}.json";

        await using var fixture = Fixture.Create();
        var serviceProvider = fixture.ServiceProvider;
        var platformSettings = serviceProvider.GetService<IOptions<PlatformSettings>>()?.Value!;

        // Act/Assert: First scope
        using (var scope = serviceProvider.CreateScope())
        {
            var altinn3LibraryCodeListService =
                scope.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
            await altinn3LibraryCodeListService.GetCachedCodeListResponseAsync(Org, CodeListId, Version);

            Assert.Equal(1, fixture.MockHandler.CallCount);
            Assert.Equal(platformSettings.Altinn3LibraryApiEndpoint + ExpectedUri, fixture.MockHandler.LastRequestUri);
        }

        // Second scope
        using (var scope = serviceProvider.CreateScope())
        {
            var altinn3LibraryCodeListService =
                scope.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
            await altinn3LibraryCodeListService.GetCachedCodeListResponseAsync(Org, codeListIdTwo, Version);

            Assert.Equal(2, fixture.MockHandler.CallCount);
            Assert.Equal(
                platformSettings.Altinn3LibraryApiEndpoint + expectedUriTwo,
                fixture.MockHandler.LastRequestUri
            );
        }
    }

    [Fact]
    public async Task GetCachedCodeListResponseAsync_RequestsWithTheSameParametersTwice_ShouldCallMessageHandlerOnce()
    {
        // Arrange
        await using var fixture = Fixture.Create();
        var serviceProvider = fixture.ServiceProvider;
        var platformSettings = serviceProvider.GetService<IOptions<PlatformSettings>>()?.Value!;

        // Act/Assert: First scope
        using (var scope = serviceProvider.CreateScope())
        {
            var altinn3LibraryCodeListService =
                scope.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
            await altinn3LibraryCodeListService.GetCachedCodeListResponseAsync(Org, CodeListId, Version);

            Assert.Equal(1, fixture.MockHandler.CallCount);
            Assert.Equal(platformSettings.Altinn3LibraryApiEndpoint + ExpectedUri, fixture.MockHandler.LastRequestUri);
        }

        // Second scope - should use cache
        using (var scope = serviceProvider.CreateScope())
        {
            var optionsProvider = scope.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
            await optionsProvider.GetCachedCodeListResponseAsync(Org, CodeListId, Version);

            // Still only 1 call because of caching
            Assert.Equal(1, fixture.MockHandler.CallCount);
            Assert.Equal(platformSettings.Altinn3LibraryApiEndpoint + ExpectedUri, fixture.MockHandler.LastRequestUri);
        }
    }

    [Fact]
    public async Task MapAppOptions_LanguageCollectionsIsEmpty_ShouldReturnOptionsWithOnlyValueAndTags()
    {
        // Arrange
        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>()
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.NotNull(option.Label);
        Assert.Empty(option.Label);
        Assert.NotNull(option.Description);
        Assert.Empty(option.Description);
        Assert.NotNull(option.HelpText);
        Assert.Empty(option.HelpText);
    }

    [Fact]
    public async Task MapAppOptions_LanguageCollectionsIsNull_ShouldReturnOptionsWithOnlyValueAndTags()
    {
        // Arrange
        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            new Dictionary<string, string>(),
            null,
            null
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.Single(result.Options);
        var option = result.Options.Single();
        Assert.Equal("", option.Label);
        Assert.Null(option.Description);
        Assert.Null(option.HelpText);
    }

    [Fact]
    public async Task MapAppOptions_NoLanguageProvided_ShouldSortAndUseFirstLanguageInDictionaryWhenNeitherNbNorEnExists()
    {
        // Arrange
        var labels = new Dictionary<string, string> { { "de", "text" }, { "se", "text" } };
        var descriptions = new Dictionary<string, string>
        {
            { "de", "Das ist ein Text" },
            { "se", "Det här är en text" },
        };
        var helpTexts = new Dictionary<string, string>
        {
            { "se", "Välj det här alternativet för att få ett text" },
            { "de", "Wählen Sie diese Option, um eine Text zu erhalten" },
        };
        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, null);

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
    public async Task MapAppOptions_NoLanguageProvided_ShouldDefaultToEnWhenNbIsNotPresentInResponseButEnIs()
    {
        // Arrange
        var labels = new Dictionary<string, string> { { "de", "text" }, { "en", "text" } };
        var descriptions = new Dictionary<string, string> { { "de", "Das ist ein Text" }, { "en", "This is a text" } };
        var helpTexts = new Dictionary<string, string>
        {
            { "en", "Choose this option to get a text" },
            { "de", "Wählen Sie diese Option, um eine Text zu erhalten" },
        };
        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, null);

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
    public async Task MapAppOptions_NoLanguageProvided_ShouldDefaultToNbWhenNbIsPresentInResponse()
    {
        // Arrange
        var altinn3LibraryCodeListResponse =
            Altinn3LibraryCodeListServiceTestData.GetNbEnAltinn3LibraryCodeListResponse();

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, null);

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
    public async Task MapAppOptions_LanguageProvidedAndPresent_ShouldReturnOptionsWithPreferredLanguage()
    {
        // Arrange
        var altinn3LibraryCodeListResponse =
            Altinn3LibraryCodeListServiceTestData.GetNbEnAltinn3LibraryCodeListResponse();

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, LanguageConst.Nb);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsCacheable);
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
    }

    [Fact]
    public async Task MapAppOptions_NoTagNamesPresent_ShouldNotReturnTagsDictionary()
    {
        // Arrange
        var labels = new Dictionary<string, string> { { "nb", "Norge" } };
        var descriptions = new Dictionary<string, string> { { "nb", "Et land på den nordlige halvkule" } };
        var helpTexts = new Dictionary<string, string> { { "nb", "" } };

        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, LanguageConst.Nb);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.NotEmpty(result.Options);
        var optionResult = result.Options.Single();
        Assert.Null(optionResult.Tags);
    }

    [Fact]
    public async Task MapAppOptions_TagNamesPresentButNoTags_ShouldNotReturnTagsDictionary()
    {
        // Arrange
        const string expectedFirstTagName = "region";
        const string expectedSecondTagName = "income";

        var tagNames = new List<string> { expectedFirstTagName, expectedSecondTagName };
        var labels = new Dictionary<string, string> { { "nb", "Norge" } };
        var descriptions = new Dictionary<string, string> { { "nb", "Et land på den nordlige halvkule" } };
        var helpTexts = new Dictionary<string, string> { { "nb", "" } };

        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts,
            tagNames
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, LanguageConst.Nb);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.NotEmpty(result.Options);
        var optionResult = result.Options.Single();
        Assert.Null(optionResult.Tags);
    }

    [Fact]
    public async Task MapAppOptions_TwoTagNamesPresentAndOneTag_ShouldNotReturnTagsDictionary()
    {
        // Arrange
        const string expectedFirstTagName = "region";
        const string expectedSecondTagName = "income";
        const string expectedFirstTag = "Europe";

        var tagNames = new List<string> { expectedFirstTagName, expectedSecondTagName };
        var tags = new List<string> { expectedFirstTag };
        var labels = new Dictionary<string, string> { { "nb", "Norge" } };
        var descriptions = new Dictionary<string, string> { { "nb", "Et land på den nordlige halvkule" } };
        var helpTexts = new Dictionary<string, string> { { "nb", "" } };

        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts,
            tagNames,
            tags
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, LanguageConst.Nb);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.NotEmpty(result.Options);
        var optionResult = result.Options.Single();
        Assert.Null(optionResult.Tags);
    }

    [Fact]
    public async Task MapAppOptions_TagNamesAndTagsPresent_ShouldMapTagNamesAndTagsToTagsDictionary()
    {
        // Arrange
        const string expectedFirstTagName = "region";
        const string expectedSecondTagName = "income";
        const string expectedFirstTag = "Europe";
        const string expectedSecondTag = "High";

        var tagNames = new List<string> { expectedFirstTagName, expectedSecondTagName };
        var tags = new List<string> { expectedFirstTag, expectedSecondTag };
        var labels = new Dictionary<string, string> { { "nb", "Norge" } };
        var descriptions = new Dictionary<string, string> { { "nb", "Et land på den nordlige halvkule" } };
        var helpTexts = new Dictionary<string, string> { { "nb", "" } };

        var altinn3LibraryCodeListResponse = Altinn3LibraryCodeListServiceTestData.GetAltinn3LibraryCodeListResponse(
            labels,
            descriptions,
            helpTexts,
            tagNames,
            tags
        );

        await using var fixture = Fixture.Create();

        // Act
        var altinn3LibraryCodeListService =
            fixture.ServiceProvider.GetRequiredService<IAltinn3LibraryCodeListService>();
        var result = altinn3LibraryCodeListService.MapAppOptions(altinn3LibraryCodeListResponse, LanguageConst.Nb);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Options);
        Assert.NotEmpty(result.Options);
        var optionResult = result.Options.Single();
        Assert.NotNull(optionResult.Tags);
        var tagsResult = optionResult.Tags;
        Assert.Equal(2, tagsResult.Count);
        var regionTagResult = tagsResult.SingleOrDefault(x => x.Key == expectedFirstTagName);
        Assert.Equal(expectedFirstTag, regionTagResult.Value);
        var incomeTagResult = tagsResult.SingleOrDefault(x => x.Key == expectedSecondTagName);
        Assert.Equal(expectedSecondTag, incomeTagResult.Value);
    }

    private sealed record Fixture(ServiceProvider ServiceProvider) : IAsyncDisposable
    {
        public required Altinn3LibraryCodeListClientMessageHandlerMock MockHandler { get; init; }

        public static Fixture Create()
        {
            var mockHandler = new Altinn3LibraryCodeListClientMessageHandlerMock(
                Altinn3LibraryCodeListServiceTestData.GetNbEnResponseMessage()
            );
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddHttpClient<IAltinn3LibraryCodeListApiClient, Altinn3LibraryCodeListApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => mockHandler);
            serviceCollection.AddTransient<IAltinn3LibraryCodeListService, Altinn3LibraryCodeListService>();
            serviceCollection.AddHybridCache();

            return new Fixture(serviceCollection.BuildServiceProvider()) { MockHandler = mockHandler };
        }

        public async ValueTask DisposeAsync() => await ServiceProvider.DisposeAsync();
    }
}
