#nullable enable
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Features.Options;

public class JoinedAppOptionsTests
{
    private readonly Mock<IAppOptionsProvider> _neverUsedOptionsProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IAppOptionsProvider> _countryAppOptionsMock = new(MockBehavior.Strict);
    private readonly Mock<IAppOptionsProvider> _sentinelOptionsProviderMock = new(MockBehavior.Strict);
    private readonly ServiceCollection _serviceCollection = new();

    private const string Language = "nb";
    private static readonly List<AppOption> AppOptionsCountries = new()
    {
        new AppOption
        {
            Value = "no",
            Label = "Norway"
        },
        new AppOption
        {
            Value = "se",
            Label = "Sweden"
        }
    };

    private static readonly List<AppOption> AppOptionsSentinel = new()
    {
        new AppOption
        {
            Value = null,
            Label = "Sentinel"
        }
    };

    public JoinedAppOptionsTests()
    {
        _countryAppOptionsMock.Setup(p => p.Id).Returns("country-no-sentinel");
        _countryAppOptionsMock
            .Setup(p => p.GetAppOptionsAsync(Language, It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync((string language, Dictionary<string, string> keyValuePairs) => new AppOptions()
            {
                Options = AppOptionsCountries,
                Parameters = keyValuePairs.ToDictionary()!,
            });
        _serviceCollection.AddSingleton(_countryAppOptionsMock.Object);

        _sentinelOptionsProviderMock.Setup(p => p.Id).Returns("sentinel");
        _sentinelOptionsProviderMock
            .Setup(p => p.GetAppOptionsAsync(Language, It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync((string language, Dictionary<string, string> keyValuePairs) => new AppOptions()
            {
                Options = AppOptionsSentinel,
                Parameters = keyValuePairs.ToDictionary()!,
            });
        _serviceCollection.AddSingleton(_sentinelOptionsProviderMock.Object);

        // This provider should never be used and cause an error if it is
        _neverUsedOptionsProviderMock.Setup(p => p.Id).Returns("never-used").Verifiable(Times.AtMost(1));

        _serviceCollection.AddSingleton<AppOptionsFactory>();

        _serviceCollection.AddJoinedAppOptions("country", "country-no-sentinel", "sentinel");
    }

    [Fact]
    public async Task JoinedOptionsProvider_ReturnsOptionsFromBothProviders()
    {
        using var sp = _serviceCollection.BuildServiceProvider();
        var factory = sp.GetRequiredService<AppOptionsFactory>();
        IAppOptionsProvider optionsProvider = factory.GetOptionsProvider("country");

        optionsProvider.Should().BeOfType<JoinedAppOptionsProvider>();
        optionsProvider.Id.Should().Be("country");
        var appOptions = await optionsProvider.GetAppOptionsAsync(Language, new Dictionary<string, string>());
        appOptions.Options.Should().HaveCount(3);
        appOptions.Options.Should().BeEquivalentTo(AppOptionsCountries.Concat(AppOptionsSentinel));

        _neverUsedOptionsProviderMock.VerifyAll();
        _countryAppOptionsMock.VerifyAll();
        _sentinelOptionsProviderMock.VerifyAll();
    }
}