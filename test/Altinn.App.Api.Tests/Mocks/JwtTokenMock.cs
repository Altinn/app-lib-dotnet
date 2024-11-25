using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Api.Tests.Mocks;

/// <summary>
/// Represents a mechanism for creating JSON Web tokens for use in integration tests.
/// </summary>
public static class JwtTokenMock
{
    /// <summary>
    /// Generates a token with a self signed certificate included in the integration test project.
    /// </summary>
    /// <param name="principal">The claims principal to include in the token.</param>
    /// <param name="tokenExpiry">How long the token should be valid for.</param>
    /// <param name="timeProvider">Alternative timeprovider, if applicable</param>
    /// <returns>A new token.</returns>
    public static string GenerateToken(
        ClaimsPrincipal principal,
        TimeSpan tokenExpiry,
        TimeProvider? timeProvider = null
    )
    {
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        var now = timeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;
        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(principal.Identity),
            Expires = now.Add(tokenExpiry).UtcDateTime,
            NotBefore = now.UtcDateTime,
            SigningCredentials = GetSigningCredentials(),
            Audience = "altinn.no",
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string tokenstring = tokenHandler.WriteToken(token);

        return tokenstring;
    }

    /// <summary>
    /// Validates a token and return the ClaimsPrincipal if successful. The validation key used is from the self signed certificate
    /// and is included in the integration test project as a separate file.
    /// </summary>
    /// <param name="token">The token to be validated.</param>
    /// <returns>ClaimsPrincipal</returns>
    public static ClaimsPrincipal ValidateToken(string token)
    {
        string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(JwtTokenMock).Assembly.Location).LocalPath)!;

        string certPath = Path.Combine(unitTestFolder, "JWTValidationCert.cer");

        X509Certificate2 cert = new(certPath);
        SecurityKey key = new X509SecurityKey(cert);

        TokenValidationParameters validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        JwtSecurityTokenHandler validator = new();
        return validator.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
    }

    private static SigningCredentials GetSigningCredentials()
    {
        string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(JwtTokenMock).Assembly.Location).LocalPath)!;

        string certPath = Path.Combine(unitTestFolder, "jwtselfsignedcert.pfx");
        X509Certificate2 cert = new X509Certificate2(certPath, "qwer1234");
        return new X509SigningCredentials(cert, SecurityAlgorithms.RsaSha256);
    }
}
