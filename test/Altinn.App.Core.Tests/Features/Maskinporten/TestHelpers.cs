using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;

namespace Altinn.App.Core.Tests.Features.Maskinporten;

public static class TestHelpers
{
    public static Mock<HttpMessageHandler> MockHttpMessageHandlerFactory(MaskinportenTokenResponse tokenResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                () =>
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
                    }
            );

        return handlerMock;
    }

    public static JsonWebKey JsonWebKeyFactory()
    {
        var rsaPubkey = new RsaSecurityKey(RSA.Create(2048).ExportParameters(true)) { KeyId = "kid1" };
        return JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaPubkey);
    }

    public static async Task<Dictionary<string, string>> ParseFormUrlEncodedContent(FormUrlEncodedContent formData)
    {
        var content = await formData.ReadAsStringAsync();
        return content
            .Split('&')
            .Select(pair => pair.Split('='))
            .ToDictionary(split => Uri.UnescapeDataString(split[0]), split => Uri.UnescapeDataString(split[1]));
    }
}
