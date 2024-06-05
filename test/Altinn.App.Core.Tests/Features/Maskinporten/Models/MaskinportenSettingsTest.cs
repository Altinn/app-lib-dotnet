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
                    "p": "5BRHaF0zpryULcbyTf02xZUXMb26Ait8XvU4NsAYCH4iLNkC_zYRJ0X_qb0sJ_WVYecB1-nCV1Qr15KnsaKp1qBOx21_ftHHwdBE12z9KYGe1xQ4ZIXEP0OiR044XQPphRFVjWOF7wQKdoXlTNXCg4B3lo5waBj8eYmMHCxyK6k",
                    "kty": "RSA",
                    "q": "yR6wLPzQ35bc0ZIxuzuil9sRpMZhqk2tWe6cV1YkqxSXPDLOjHBPbwdeNdd2qdSY_x0myzIF_KA0xD-Q5YXCMt-8UxJTYKf8TLIxTdRKyW7KQbR0HJ4yNx0DuoXEeeDfLXbMX_TbL_W6N4xHPHiuGvh1Spr4s4JC0Ka1PLK8b2E",
                    "d": "jR4l-ZW3_eAqTRxmmkYNTGxp8fURn8C9mar5-NatcyR5HfqpofjQIVtGLNfhS-YMkeam8pIXjsdrWrSTIC22uUf4OuNDRWsKEwePYoNO1xNusF-8fOMM7At6qtPpcXk3pEHfEuSjplIOAL9scj2oeF3jqe5eP9l4KHDYLugqkxJz3AoObTBQDykXx3uq_3cjeSBss1XFdEnpD2Br1zR7-sGaEoSIQyT6a8Ulgr1Ah5AHm6KX4LTgPx3NuWLyDqN-L6QxYnv27BC6J-4ehwpf84CO-uolJKcVPvEwBf35LFlBA3JgKaVYyC7SZ5A2y_EBViKhubgOuMm9_2C7o9PyAQ",
                    "e": "AQAB",
                    "use": "sig",
                    "kid": "test-kid",
                    "qi": "E-oyO4HWOVxD_d7xZxFltTZDz6ZtLPZFB_KYXYeVFDrO8KZE9kFb4TNlFvrFjv-dHtpNey95gqtOtdNMwdVdZbAKbDdo5LYSJ_rk-4ZVsDusq7FCJ5nmmrxfQ5yNEPqHwgdUfs50v_fV1x8SEDjfWzaaVK5ltqPiUXtpTTLBQIg",
                    "dp": "yVvJ6y6VgjfszjldBFNv_qHwlz58MJw5sg_mcBfJX_4Tp-pzReNy42xeGXnkuOaM2qE6tGcw5y5tgmV8XUxRiyV-R3y5WbpVFBwOGu6i1vkTxaiZXM3oAz5vz2oUQrJIgO1bzXa28NxtbFQrq1jw4G4Tpjzcqlqc06QGqXzn0vk",
                    "alg": "RS256",
                    "dq": "B5CI7dhAfvhsq9FE35b5oZ6SxlDT4ZT0XTqVVM-fp3Op0JDUpgGfazyqtXm6M98UNhxBlkj2Yq8f7PW7HHbwe_tgWPuKeUs4OSZGpnfCrFrnbps79suYdew4dK6NWkwz-MDMJRvPlrk2XNqA32xmmAsaVkkH67CNlM2AaZ0La2E",
                    "n": "sy9DZ1U6jfP1UBN2EZTD1DPkajdZsFsXGGVHfbJmH5MFwXbtKlwV_jYjz58YIj1n48OxH7f-Ldgc-fBLz45QU1HbDZPij7q3uYm1ZMTGkPqkY8kHX51TsFOEqzVhNfyc6yVsjlj5KPyyxLyAcx6ixiE2K8vIeuKMVbZCZt605L39ENUsiQ-cfnqp-zo1ihU5xJOQaWV9pGuG4XoLAUIktF6_YPF4pFmSWRHk5aURUfTCvo11n3EUBYJUiJb8AqUt3yqGSoV-4wXir-9oRNjDUtE_QA3eErGKCebtUd6oxzcXcHiGY0npKxt7JQti3jTZRcnkScmmP-XvrQzB6kzSCQ"
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

    [Fact]
    public void ShouldValidateJwkAfterDeserializing()
    {
        // Arrange

        // In this case, key.e has empty string value. Which should trigger a JsonException
        var json = """
            {
                "authority": "https://maskinporten.dev/",
                "clientId": "test-client",
                "key": {
                    "p": "5BRHaF0zpryULcbyTf02xZUXMb26Ait8XvU4NsAYCH4iLNkC_zYRJ0X_qb0sJ_WVYecB1-nCV1Qr15KnsaKp1qBOx21_ftHHwdBE12z9KYGe1xQ4ZIXEP0OiR044XQPphRFVjWOF7wQKdoXlTNXCg4B3lo5waBj8eYmMHCxyK6k",
                    "kty": "RSA",
                    "q": "yR6wLPzQ35bc0ZIxuzuil9sRpMZhqk2tWe6cV1YkqxSXPDLOjHBPbwdeNdd2qdSY_x0myzIF_KA0xD-Q5YXCMt-8UxJTYKf8TLIxTdRKyW7KQbR0HJ4yNx0DuoXEeeDfLXbMX_TbL_W6N4xHPHiuGvh1Spr4s4JC0Ka1PLK8b2E",
                    "d": "jR4l-ZW3_eAqTRxmmkYNTGxp8fURn8C9mar5-NatcyR5HfqpofjQIVtGLNfhS-YMkeam8pIXjsdrWrSTIC22uUf4OuNDRWsKEwePYoNO1xNusF-8fOMM7At6qtPpcXk3pEHfEuSjplIOAL9scj2oeF3jqe5eP9l4KHDYLugqkxJz3AoObTBQDykXx3uq_3cjeSBss1XFdEnpD2Br1zR7-sGaEoSIQyT6a8Ulgr1Ah5AHm6KX4LTgPx3NuWLyDqN-L6QxYnv27BC6J-4ehwpf84CO-uolJKcVPvEwBf35LFlBA3JgKaVYyC7SZ5A2y_EBViKhubgOuMm9_2C7o9PyAQ",
                    "e": "",
                    "use": "sig",
                    "kid": "test-kid",
                    "qi": "E-oyO4HWOVxD_d7xZxFltTZDz6ZtLPZFB_KYXYeVFDrO8KZE9kFb4TNlFvrFjv-dHtpNey95gqtOtdNMwdVdZbAKbDdo5LYSJ_rk-4ZVsDusq7FCJ5nmmrxfQ5yNEPqHwgdUfs50v_fV1x8SEDjfWzaaVK5ltqPiUXtpTTLBQIg",
                    "dp": "yVvJ6y6VgjfszjldBFNv_qHwlz58MJw5sg_mcBfJX_4Tp-pzReNy42xeGXnkuOaM2qE6tGcw5y5tgmV8XUxRiyV-R3y5WbpVFBwOGu6i1vkTxaiZXM3oAz5vz2oUQrJIgO1bzXa28NxtbFQrq1jw4G4Tpjzcqlqc06QGqXzn0vk",
                    "alg": "RS256",
                    "dq": "B5CI7dhAfvhsq9FE35b5oZ6SxlDT4ZT0XTqVVM-fp3Op0JDUpgGfazyqtXm6M98UNhxBlkj2Yq8f7PW7HHbwe_tgWPuKeUs4OSZGpnfCrFrnbps79suYdew4dK6NWkwz-MDMJRvPlrk2XNqA32xmmAsaVkkH67CNlM2AaZ0La2E",
                    "n": "sy9DZ1U6jfP1UBN2EZTD1DPkajdZsFsXGGVHfbJmH5MFwXbtKlwV_jYjz58YIj1n48OxH7f-Ldgc-fBLz45QU1HbDZPij7q3uYm1ZMTGkPqkY8kHX51TsFOEqzVhNfyc6yVsjlj5KPyyxLyAcx6ixiE2K8vIeuKMVbZCZt605L39ENUsiQ-cfnqp-zo1ihU5xJOQaWV9pGuG4XoLAUIktF6_YPF4pFmSWRHk5aURUfTCvo11n3EUBYJUiJb8AqUt3yqGSoV-4wXir-9oRNjDUtE_QA3eErGKCebtUd6oxzcXcHiGY0npKxt7JQti3jTZRcnkScmmP-XvrQzB6kzSCQ"
                }
            }
            """;

        // Act
        System.Action act = () => JsonSerializer.Deserialize<MaskinportenSettings>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }
}
