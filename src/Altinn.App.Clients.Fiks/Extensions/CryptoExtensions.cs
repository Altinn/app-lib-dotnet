using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class CryptoExtensions
{
    public static (RSA publicKey, RSA? privateKey) ConvertJwkToRsa(this JsonWebKey jwk)
    {
        if (jwk.Kty != JsonWebAlgorithmsKeyTypes.RSA)
            throw new ArgumentException("The provided JWK is not an RSA key.");

        var rsaParams = new RSAParameters { Modulus = Base64UrlDecode(jwk.N), Exponent = Base64UrlDecode(jwk.E) };

        RSA publicKey = RSA.Create();
        publicKey.ImportParameters(rsaParams);

        RSA? privateKey = null;

        if (jwk.HasPrivateKey)
        {
            rsaParams.D = Base64UrlDecode(jwk.D);
            rsaParams.P = Base64UrlDecode(jwk.P);
            rsaParams.Q = Base64UrlDecode(jwk.Q);
            rsaParams.DP = Base64UrlDecode(jwk.DP);
            rsaParams.DQ = Base64UrlDecode(jwk.DQ);
            rsaParams.InverseQ = Base64UrlDecode(jwk.QI);

            privateKey = RSA.Create();
            privateKey.ImportParameters(rsaParams);
        }

        return (publicKey, privateKey);

        static byte[] Base64UrlDecode(string input) => Base64UrlEncoder.DecodeBytes(input);
    }
}
