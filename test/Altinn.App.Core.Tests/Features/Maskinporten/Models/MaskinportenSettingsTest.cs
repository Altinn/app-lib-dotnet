using System.Text.Json;
using Altinn.App.Core.Features.Maskinporten.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Maskinporten.Models;

public class MaskinportenSettingsTest
{
    [Fact]
    public void ShouldDeserializeFromJsonCorrectly()
    {
        // Arrange
        var json = """
            {
                "authority": "https://maskinporten.dev/",
                "clientId": "test-client",
                "key": {
                    "p": "zcLJxgdR1fJSeldQ98TYqms9JAVTvnKZMKFuFEMLhvYxeN56TQ4pxhVgjIYAT7bOMP0HCer1GckoQi6JU8kCLt5QqxFoYQKOytMWYaDVyDFjNswsj02yiDOpaP3gKAz8ei3rVgK_LSPR3jcBV2x68b_Q6PvEMzuX5JWIFsyxPEs",
                    "kty": "RSA",
                    "q": "xmF9b3zrcU76hDw06qT5GlD2HpRQbLK15Px4UwIprZPNQaAoDj6Y4ByUoPPtDatcJQLfXGq-kJsZLUJi0r814AqBpirAF5aMW68sBU-ZocNegQllAxspdCVDcLlcHGTAzIRKQoJ2_URp_nv3HpG6Chym9tZ0nQds_kKZSvhPv9U",
                    "d": "KUrTzXhxdQ2cSnLKF86hF6cK0UgWPuq-_IWiGzNIGb5DlTfS_UZnQaV1zkwuZlQG2CwOQ5oMdvSS5vQMuipnJtgxgmtGwRkDb-Be4-dCZ7d_WVhY_zxWoPziWWRNsuaxJu3ZJldYvtAY9iviAZgLq0UdenkCHyfb_eBgo9-srqb15f1FUUEtSPhBBJ_Jgfa-NJz4zknkO5NFe8SF4B0LTGkshPGAq2IXnuKaJ077w4JHFMPwRNclePH0PwA6Vkb85msM8FAEwutQPOGQOoleXNNAcRr18tLukmSim23AaATq1-VWnzYFvTxhXdxrQvjEsW4cSQoVmJuJSWi5xpYJYQ",
                    "e": "AQAB",
                    "use": "sig",
                    "kid": "test-kid",
                    "qi": "CEMMcrchDDfgy8beXlxg7eR7jWL8quO_UyhMHrCBER_h4yMq3m8Vs5ywFHXMgtk7d_n9IVOYb-g5K7JbEBRcoliTFsy28qOIFF_Mau6oLFpOTZpIYE5XPxqYjib1k3P13Ls8Gr8E1euVXYqCseEs8VpuCLetzkmYvbS_JQzoxAc",
                    "dp": "awrAyWKZckHkIn02RA-F3_J2Fj0nOdaIV0JD8AqI_qcpSYYD_f42QTRxy-kSVGX3koivlrtC0y1Q4k0vaAUUO6mwMa6WrJEWE_IInLV9Qe5ffOxu6gYzsKOfqF0atfs7hZxJ5676IdOWpJHdAswOkaGGXw5LHZQNCv-3kpTeR00",
                    "alg": "RS256",
                    "dq": "mgqPhSeiBt0F5_J8QIuTplyhkEMoZA8s8L8ei45NYKw5ILFFCTKQMM3gl05xj0C1j0O4vyFgEhdtKFi5Nd4l7m8aFzZw7KAJIxRVgVY6_IIg-t-ZoRzRRo-822YDYUTW9Lfwlc6KqMUb7PDDhegwZrOy7k6-RETE3HZIZA-GlXU",
                    "n": "n3MDr1YU1ok4p7ds8XhhmHe9weYrbQoqOSVyd6oiGsOQjdZ-LSa-LPXLkMXZD8iw2Aq3SqOCCU6k4zRdnMvzeB3Ph8PNvzJypjgXtzVe5Bf_xXiGDq50pZx_hYATqDgNPNzTedK8N2ZicKhWHq5TbrnXkc5agSVlBsNXXk7OMtznTBYlgXD2j0a_5tt4nN3HFBhuwQUqFPNkR1mbaLqJNeVIAusVkPrtKdiYPLRlFWPfuq5svfOI2NBD9Ec3OOYcgYchcLlZz7XGhPD3kBrWl3F9-ot2vi-R_1D19A7kqNhJ92p8p25aAAHrdF6-v1a0sw-FbVFZYfzj4lt4zpgfZw"
                }
            }
            """;

        // Act
        var settings = JsonSerializer.Deserialize<MaskinportenSettings>(json);

        // Assert
        Assert.NotNull(settings);
        settings.Authority.Should().Be("https://maskinporten.dev/");
        settings.ClientId.Should().Be("test-client");
        settings.Key.KeyId.Should().Be("test-kid");
    }
}
