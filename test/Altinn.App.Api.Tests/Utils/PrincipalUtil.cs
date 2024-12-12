using System.Security.Claims;
using Altinn.App.Api.Tests.Mocks;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Models;
using AltinnCore.Authentication.Constants;

namespace Altinn.App.Api.Tests.Utils;

public static class TestAuthentication
{
    internal const int DefaultUserId = 5000;
    internal const int DefaultUserPartyId = 5000;
    internal const int DefaultUserAuthenticationLevel = 2;

    internal const string DefaultOrgNumber = "405003309";
    internal const string DefaultOrg = "ttd";
    internal const string DefaultOrgScope = "altinn:serviceowner/instances.read altinn:serviceowner/instances.write";

    public static string GetUserToken(
        int userId = DefaultUserId,
        int partyId = DefaultUserPartyId,
        int authenticationLevel = DefaultUserAuthenticationLevel
    )
    {
        ClaimsPrincipal principal = GetUserPrincipal(userId, partyId, authenticationLevel);
        string token = JwtTokenMock.GenerateToken(principal, TimeSpan.FromMinutes(10));
        return token;
    }

    public static ClaimsPrincipal GetUserPrincipal(
        int userId = DefaultUserId,
        int partyId = DefaultUserPartyId,
        int authenticationLevel = DefaultUserAuthenticationLevel
    )
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";

        var sessionId = Guid.NewGuid().ToString();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, $"user-{userId}-{partyId}", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserName, $"User{userId}", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
        claims.Add(new Claim("jti", sessionId, ClaimValueTypes.String, issuer));
        claims.Add(
            new Claim(
                AltinnCoreClaimTypes.AuthenticationLevel,
                authenticationLevel.ToString(),
                ClaimValueTypes.Integer32,
                issuer
            )
        );

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        return principal;
    }

    public static ClaimsPrincipal GetOrgPrincipal(
        string orgNumber = DefaultOrgNumber,
        string scope = DefaultOrgScope,
        string? org = DefaultOrg
    )
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";
        claims.Add(new Claim(AltinnCoreClaimTypes.OrgNumber, orgNumber, ClaimValueTypes.String, issuer));

        if (scope.Contains("altinn:serviceowner") && !string.IsNullOrWhiteSpace(org))
            claims.Add(new Claim(AltinnCoreClaimTypes.Org, org, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, issuer));

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);

        return new ClaimsPrincipal(identity);
    }

    public static string GetOrgToken(
        string orgNumber = DefaultOrgNumber,
        string scope = DefaultOrgScope,
        string? org = DefaultOrg,
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        ClaimsPrincipal principal = GetOrgPrincipal(orgNumber, scope, org);
        return JwtTokenMock.GenerateToken(principal, expiry ?? TimeSpan.FromMinutes(2), timeProvider);
    }

    internal static MaskinportenTokenResponse GetMaskinportenToken(
        string scope,
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        List<Claim> claims = [];
        const string issuer = "https://test.maskinporten.no/";
        claims.Add(new Claim(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(JwtClaimTypes.Maskinporten.AuthenticationMethod, "Mock", ClaimValueTypes.String, issuer));

        ClaimsIdentity identity = new("mock");
        identity.AddClaims(claims);
        ClaimsPrincipal principal = new(identity);
        expiry ??= TimeSpan.FromMinutes(2);
        string accessToken = JwtTokenMock.GenerateToken(principal, expiry.Value, timeProvider);

        return new MaskinportenTokenResponse
        {
            AccessToken = JwtToken.Parse(accessToken),
            ExpiresIn = (int)expiry.Value.TotalSeconds,
            Scope = scope,
            TokenType = "Bearer",
        };
    }

    public static string GetSelfIdentifiedUserToken(string username, string partyId, string userId)
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";
        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString(), ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserName, username, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "0", ClaimValueTypes.Integer32, issuer));

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        string token = JwtTokenMock.GenerateToken(principal, new TimeSpan(1, 1, 1));

        return token;
    }
}
