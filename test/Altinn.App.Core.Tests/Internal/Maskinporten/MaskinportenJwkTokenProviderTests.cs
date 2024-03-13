using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.App.Core.Internal.Maskinporten;
using Altinn.App.Core.Internal.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Maskinporten;

public class MaskinportenJwkTokenProviderTests
{
    [Fact]
    public async Task GetToken_ShouldReturnToken()
    {
        var maskinportenService = new Mock<IMaskinportenService>();
        maskinportenService.Setup(s => s.GetToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new TokenResponse { AccessToken = "myMaskinportenAccessToken" });

        IOptions<MaskinportenSettings> maskinportenSettings = Options.Create(new MaskinportenSettings()
        {
            Environment = "ver2",
            ClientId = Guid.NewGuid().ToString()
        });
        var secretsClient = new Mock<ISecretsClient>();
        secretsClient.Setup(s => s.GetSecretAsync(It.IsAny<string>())).ReturnsAsync("myBase64EncodedJwk");

        MaskinportenJwkTokenProvider maskinportenJwkTokenProvider = new(maskinportenService.Object, maskinportenSettings, secretsClient.Object, "nameOfMySecretInKeyVault");

        string scopes = "altinn:serviceowner/instances.read";
        string token = await maskinportenJwkTokenProvider.GetToken(scopes);

        token.Should().NotBeNullOrEmpty();
        maskinportenService.Verify(s => s.GetToken("myBase64EncodedJwk", maskinportenSettings.Value.Environment, maskinportenSettings.Value.ClientId, scopes, string.Empty, null, false));
        secretsClient.Verify(s => s.GetSecretAsync("nameOfMySecretInKeyVault"));
    }
}
