using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Texts;

public class TranslationServiceTests
{
    private readonly Mock<IAppResources> _appResourcesMock = new(MockBehavior.Loose);
    private readonly TranslationService _translationService;

    public TranslationServiceTests()
    {
        _appResourcesMock
            .Setup(appResources =>
                appResources.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s == LanguageConst.Nb))
            )
            .ReturnsAsync(new TextResource { Resources = [new TextResourceElement { Id = "text", Value = "bokmål" }] });

        _appResourcesMock
            .Setup(appResources =>
                appResources.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(s => s == LanguageConst.En))
            )
            .ReturnsAsync(
                new TextResource { Resources = [new TextResourceElement { Id = "text", Value = "english" }] }
            );

        _translationService = new TranslationService(new AppIdentifier("org", "app"), _appResourcesMock.Object);
    }

    [Fact]
    public async Task Returns_Nb()
    {
        var result = await _translationService.TranslateTextKey("text", LanguageConst.Nb);
        Assert.Equal("bokmål", result);
    }

    [Fact]
    public async Task Returns_En()
    {
        var result = await _translationService.TranslateTextKey("text", LanguageConst.En);
        Assert.Equal("english", result);
    }

    [Fact]
    public async Task Default_Nb()
    {
        var result = await _translationService.TranslateTextKey("text", null);
        Assert.Equal("bokmål", result);
    }

    [Fact]
    public async Task Fallback_Nb()
    {
        var result = await _translationService.TranslateTextKey("text", LanguageConst.Nn);
        Assert.Equal("bokmål", result);
    }

    [Fact]
    public async Task Fail_Missing()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _translationService.TranslateTextKey("missing", LanguageConst.Nb)
        );
    }

    [Fact]
    public async Task Lenient_Returns_Null_If_Key_Is_Null()
    {
        var result = await _translationService.TranslateTextKeyLenient(null, LanguageConst.Nb);
        Assert.Null(result);
    }
}
