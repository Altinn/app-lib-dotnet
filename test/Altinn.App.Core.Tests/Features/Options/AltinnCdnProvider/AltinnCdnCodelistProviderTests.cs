using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options.AltinnCdnProvider;
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Options.AltinnCdnProvider;

public class AltinnCdnCodelistProviderTests
{
    [Fact]
    public async Task GetAppOptionsAsync_ShouldReturnCodeList()
    {
        var httpClientMock = new AltinnCdnApiClientMock();
        IAppOptionsProvider appOptionsProvider = new AltinnCdnCodelistProvider(httpClientMock);
        var queryParameters = new Dictionary<string, string>
        {
            { "orgName", "ttd" },
            { "codeListId", "yesNoMaybe" },
            { "version", "1970-01-01T00-00-00Z" },
        };

        var appOptions = await appOptionsProvider.GetAppOptionsAsync("en", queryParameters);

        appOptions.Options.Should().HaveCount(3);
        var yesOption = appOptions.Options!.First(x => x.Value == "yes");
        yesOption.Value.Should().Be("yes");
        yesOption.Label.Should().Be("Yes");
        yesOption.Description.Should().Be("Description for yes");
        yesOption.HelpText.Should().Be("Help text for yes");
    }

    [Fact]
    public async Task GetAppOptionsAsync_ValueTypeShouldBeNumber_WhenValueIsNumber()
    {
        var httpClientMock = new AltinnCdnApiClientMock();
        IAppOptionsProvider appOptionsProvider = new AltinnCdnCodelistProvider(httpClientMock);
        var queryParameters = new Dictionary<string, string>
        {
            { "orgName", "ttd" },
            { "codeListId", "numbers" },
            { "version", "1970-01-01T00-00-00Z" },
        };

        var appOptions = await appOptionsProvider.GetAppOptionsAsync("en", queryParameters);

        var firstOption = appOptions.Options![0];
        firstOption.Value.Should().Be("1");
        firstOption.ValueType.Should().Be(AppOptionValueType.Number);
    }
}
