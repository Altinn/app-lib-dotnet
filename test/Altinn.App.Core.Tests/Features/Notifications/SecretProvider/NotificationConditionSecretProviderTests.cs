using Altinn.App.Core.Features.Notifications.Exceptions;
using Altinn.App.Core.Features.Notifications.SecretProvider;
using Altinn.App.Core.Infrastructure.Clients.Secrets;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.Features.Notifications.SecretProvider;

public class NotificationConditionSecretProviderTests
{
    private readonly Mock<IOptionsMonitor<AppCodesSettings>> _optionsMonitorMock = new(MockBehavior.Strict);

    private NotificationConditionSecretProvider CreateSut() => new(_optionsMonitorMock.Object);

    private void SetupCodes(List<string> codes) =>
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(new AppCodesSettings { NotificationCallback = codes });

    [Fact]
    public void GetSigningSecret_ReturnFirstCode()
    {
        SetupCodes(["first-secret", "second-secret"]);

        var result = CreateSut().GetSigningSecret();

        Assert.Equal("first-secret", result);
    }

    [Fact]
    public void GetSigningSecret_SingleCode_ReturnsIt()
    {
        SetupCodes(["only-secret"]);

        var result = CreateSut().GetSigningSecret();

        Assert.Equal("only-secret", result);
    }

    [Fact]
    public void GetSigningSecret_EmptyCodes_Throws()
    {
        SetupCodes([]);

        Assert.Throws<NotificationConditionSecretNotFoundException>(() => CreateSut().GetSigningSecret());
    }

    [Fact]
    public void GetValidationSecrets_ReturnsAllCodes()
    {
        var codes = new List<string> { "first-secret", "second-secret", "third-secret" };
        SetupCodes(codes);

        var result = CreateSut().GetValidationSecrets();

        Assert.Equal(codes, result);
    }

    [Fact]
    public void GetValidationSecrets_EmptyCodes_Throws()
    {
        SetupCodes([]);

        Assert.Throws<NotificationConditionSecretNotFoundException>(() => CreateSut().GetValidationSecrets());
    }

    [Fact]
    public void GetValidationSecrets_SingleCode_ReturnsSingleItemList()
    {
        SetupCodes(["only-secret"]);

        var result = CreateSut().GetValidationSecrets();

        Assert.Single(result);
        Assert.Equal("only-secret", result[0]);
    }

    [Fact]
    public void GetSigningSecret_AlwaysReadsCurrentValue()
    {
        SetupCodes(["secret"]);

        CreateSut().GetSigningSecret();

        _optionsMonitorMock.Verify(x => x.CurrentValue, Times.Once);
    }

    [Fact]
    public void GetValidationSecrets_AlwaysReadsCurrentValue()
    {
        SetupCodes(["secret"]);

        CreateSut().GetValidationSecrets();

        _optionsMonitorMock.Verify(x => x.CurrentValue, Times.Once);
    }
}
