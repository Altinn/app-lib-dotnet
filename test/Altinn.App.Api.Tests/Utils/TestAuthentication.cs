using System.Security.Claims;
using System.Text.Json;
using Altinn.App.Api.Tests.Mocks;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using AltinnCore.Authentication.Constants;
using Authorization.Platform.Authorization.Models;
using static Altinn.App.Core.Features.Auth.Authenticated;

namespace Altinn.App.Api.Tests.Utils;

public enum AuthenticationTypes
{
    User,
    SelfIdentifiedUser,
    Org,
    SystemUser,
    ServiceOwner,
}

public sealed record TestJwtToken(AuthenticationTypes Type, int PartyId, string Token, Authenticated Auth);

public static class TestAuthentication
{
    internal const int DefaultUserId = 1337;
    internal const int DefaultUserPartyId = 501337;
    internal const string DefaultUsername = "testuser";
    internal const int DefaultUserAuthenticationLevel = 2;

    internal const string DefaultOrgNumber = "405003309";
    internal const string DefaultOrg = "tdd";
    internal const int DefaultOrgPartyId = 5001337;
    internal const string DefaultServiceOwnerScope =
        "altinn:serviceowner/instances.read altinn:serviceowner/instances.write";
    internal const string DefaultOrgScope = "altinn:instances.read altinn:instances.write";

    internal const string DefaultSystemUserId = "f58fe166-bc22-4899-beb7-c3e8e3332f43";
    internal const string DefaultSystemId = "1cb8b115-31bf-421f-8029-8bb0cd23c954";

    public sealed class AllTokens : TheoryData<TestJwtToken>
    {
        public AllTokens()
        {
            Add(new(AuthenticationTypes.User, DefaultUserPartyId, GetUserToken(), GetUserAuthentication()));
            Add(
                new(
                    AuthenticationTypes.SelfIdentifiedUser,
                    DefaultUserPartyId,
                    GetSelfIdentifiedUserToken(),
                    GetSelfIdentifiedUserAuthentication()
                )
            );
            // Add(new(AuthenticationTypes.Org, DefaultOrgPartyId, GetOrgAuthentication()));
            Add(
                new(
                    AuthenticationTypes.ServiceOwner,
                    DefaultOrgPartyId,
                    GetServiceOwnerToken(),
                    GetServiceOwnerAuthentication()
                )
            );
            Add(
                new(
                    AuthenticationTypes.SystemUser,
                    DefaultOrgPartyId,
                    GetSystemUserToken(),
                    GetSystemUserAuthentication()
                )
            );
        }
    }

    public sealed class AllTypes : TheoryData<AuthenticationTypes>
    {
        public AllTypes()
        {
            Add(AuthenticationTypes.User);
            Add(AuthenticationTypes.SelfIdentifiedUser);
            // Add(AuthenticationTypes.Org);
            Add(AuthenticationTypes.ServiceOwner);
            Add(AuthenticationTypes.SystemUser);
        }
    }

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

    public static User GetUserAuthentication(
        int userId = DefaultUserId,
        int userPartyId = DefaultUserPartyId,
        int authenticationLevel = DefaultUserAuthenticationLevel,
        string? email = null,
        string? ssn = null,
        ProfileSettingPreference? profileSettingPreference = null
    )
    {
        var party = new Party()
        {
            PartyId = userPartyId,
            PartyTypeName = PartyType.Person,
            OrgNumber = null,
            SSN = ssn ?? "12345678901",
            Name = "Test Testesen",
        };
        return new User(
            userId,
            userPartyId,
            authenticationLevel,
            "IDporten",
            userPartyId,
            "",
            getUserProfile: uid =>
            {
                Assert.Equal(userId, uid);
                return Task.FromResult<UserProfile?>(
                    new UserProfile()
                    {
                        UserId = userId,
                        PartyId = userPartyId,
                        Party = party,
                        Email = email ?? "test@testesen.no",
                        ProfileSettingPreference = profileSettingPreference,
                    }
                );
            },
            lookupParty: partyId =>
            {
                Assert.Equal(userPartyId, partyId);
                return Task.FromResult<Party?>(party);
            },
            getPartyList: uid =>
            {
                Assert.Equal(userId, uid);
                return Task.FromResult<List<Party>?>([party]);
            },
            validateSelectedParty: (uid, pid) =>
            {
                Assert.Equal(userId, uid);
                Assert.Equal(userPartyId, pid);
                return Task.FromResult<bool?>(true);
            },
            getUserRoles: (uid, pid) =>
            {
                Assert.Equal(userId, uid);
                Assert.Equal(userPartyId, pid);
                return Task.FromResult<IEnumerable<Role>>([]);
            },
            getApplicationMetadata: _getApplicationMetadata
        );
    }

    public static string GetSelfIdentifiedUserToken(
        string username = DefaultUsername,
        int userId = DefaultUserId,
        int partyId = DefaultUserPartyId
    )
    {
        ClaimsPrincipal principal = GetSelfIdentifiedUserPrincipal(username, userId, partyId);
        string token = JwtTokenMock.GenerateToken(principal, TimeSpan.FromMinutes(10));
        return token;
    }

    public static ClaimsPrincipal GetSelfIdentifiedUserPrincipal(
        string username = DefaultUsername,
        int userId = DefaultUserId,
        int partyId = DefaultUserPartyId
    )
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";

        var sessionId = Guid.NewGuid().ToString();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, $"user-{userId}-{partyId}", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.UserName, username, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
        claims.Add(new Claim("jti", sessionId, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "0", ClaimValueTypes.Integer32, issuer));

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        return principal;
    }

    public static SelfIdentifiedUser GetSelfIdentifiedUserAuthentication(
        string username = DefaultUsername,
        int userId = DefaultUserId,
        int partyId = DefaultUserPartyId,
        string? email = null,
        ProfileSettingPreference? profileSettingPreference = null
    )
    {
        var party = new Party()
        {
            PartyId = partyId,
            PartyTypeName = PartyType.SelfIdentified,
            OrgNumber = null,
            Name = "Test Testesen",
        };
        return new SelfIdentifiedUser(
            username,
            userId,
            partyId,
            "IDporten",
            "",
            getUserProfile: uid =>
            {
                Assert.Equal(userId, uid);
                return Task.FromResult<UserProfile?>(
                    new UserProfile()
                    {
                        UserId = userId,
                        UserName = username,
                        PartyId = partyId,
                        Party = party,
                        Email = email ?? "test@testesen.no",
                        ProfileSettingPreference = profileSettingPreference,
                    }
                );
            },
            getApplicationMetadata: _getApplicationMetadata
        );
    }

    public static ClaimsPrincipal GetOrgPrincipal(string orgNumber = DefaultOrgNumber, string scope = DefaultOrgScope)
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";
        claims.Add(new Claim(AltinnCoreClaimTypes.OrgNumber, orgNumber, ClaimValueTypes.String, issuer));

        if (scope.Contains("altinn:serviceowner"))
            throw new InvalidOperationException("Org token cannot have serviceowner scopes");

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
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        ClaimsPrincipal principal = GetOrgPrincipal(orgNumber, scope);
        return JwtTokenMock.GenerateToken(principal, expiry ?? TimeSpan.FromMinutes(2), timeProvider);
    }

    public static Org GetOrgAuthentication(
        string orgNumber = DefaultOrgNumber,
        int partyId = DefaultOrgPartyId,
        int authenticationLevel = DefaultUserAuthenticationLevel
    )
    {
        var party = new Party()
        {
            PartyId = partyId,
            PartyTypeName = PartyType.Organisation,
            OrgNumber = orgNumber,
            Name = "Test AS",
        };
        return new Org(
            orgNumber,
            authenticationLevel,
            "maskinporten",
            "",
            lookupParty: orgNo =>
            {
                Assert.Equal(orgNumber, orgNo);
                return Task.FromResult(party);
            },
            getApplicationMetadata: _getApplicationMetadata
        );
    }

    private static readonly Func<Task<ApplicationMetadata>> _getApplicationMetadata = () =>
    {
        return Task.FromResult(
            new ApplicationMetadata("ttd/app")
            {
                PartyTypesAllowed = new PartyTypesAllowed()
                {
                    BankruptcyEstate = true,
                    Organisation = true,
                    Person = true,
                    SubUnit = true,
                },
            }
        );
    };

    public static ClaimsPrincipal GetServiceOwnerPrincipal(
        string orgNumber = DefaultOrgNumber,
        string scope = DefaultServiceOwnerScope,
        string org = DefaultOrg
    )
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";
        claims.Add(new Claim(AltinnCoreClaimTypes.OrgNumber, orgNumber, ClaimValueTypes.String, issuer));

        if (scope.Contains("altinn:serviceowner"))
            claims.Add(new Claim(AltinnCoreClaimTypes.Org, org, ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, issuer));

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);

        return new ClaimsPrincipal(identity);
    }

    public static string GetServiceOwnerToken(
        string orgNumber = DefaultOrgNumber,
        string scope = DefaultServiceOwnerScope,
        string org = DefaultOrg,
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        ClaimsPrincipal principal = GetServiceOwnerPrincipal(orgNumber, scope, org);
        return JwtTokenMock.GenerateToken(principal, expiry ?? TimeSpan.FromMinutes(2), timeProvider);
    }

    public static ServiceOwner GetServiceOwnerAuthentication(
        string orgNumber = DefaultOrgNumber,
        string org = DefaultOrg,
        int partyId = DefaultOrgPartyId,
        int authenticationLevel = DefaultUserAuthenticationLevel
    )
    {
        var party = new Party()
        {
            PartyId = partyId,
            PartyTypeName = PartyType.Organisation,
            OrgNumber = orgNumber,
            Name = "Test AS",
        };
        return new ServiceOwner(
            org,
            orgNumber,
            authenticationLevel,
            "maskinporten",
            "",
            lookupParty: orgNo =>
            {
                Assert.Equal(orgNumber, orgNo);
                return Task.FromResult(party);
            }
        );
    }

    public static ClaimsPrincipal GetSystemUserPrincipal(
        string systemId = DefaultSystemId,
        string systemUserId = DefaultSystemUserId,
        string systemUserOrgNumber = DefaultOrgNumber,
        string scope = DefaultServiceOwnerScope
    )
    {
        List<Claim> claims = [];
        string issuer = "www.altinn.no";

        AuthorizationDetailsClaim details = new SystemUserAuthorizationDetailsClaim(
            [systemUserId],
            systemId,
            new SystemUserOrg(
                "iso6523-actorid-upis",
                OrganisationNumber.Parse(systemUserOrgNumber).Get(OrganisationNumberFormat.International)
            )
        );
        claims.Add(
            new Claim("authorization_details", JsonSerializer.Serialize(details), ClaimValueTypes.String, issuer)
        );
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticateMethod, "systemuser", ClaimValueTypes.String, issuer));
        claims.Add(new Claim(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, issuer));
        claims.Add(new Claim(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, issuer));

        ClaimsIdentity identity = new ClaimsIdentity("mock");
        identity.AddClaims(claims);

        return new ClaimsPrincipal(identity);
    }

    public static string GetSystemUserToken(
        string systemId = DefaultSystemId,
        string systemUserId = DefaultSystemUserId,
        string systemUserOrgNumber = DefaultOrgNumber,
        string scope = DefaultServiceOwnerScope,
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        ClaimsPrincipal principal = GetSystemUserPrincipal(systemId, systemUserId, systemUserOrgNumber, scope);
        return JwtTokenMock.GenerateToken(principal, expiry ?? TimeSpan.FromMinutes(2), timeProvider);
    }

    public static SystemUser GetSystemUserAuthentication(
        string systemId = DefaultSystemId,
        string systemUserId = DefaultSystemUserId,
        string systemUserOrgNumber = DefaultOrgNumber,
        int partyId = DefaultOrgPartyId
    )
    {
        var party = new Party()
        {
            PartyId = partyId,
            PartyTypeName = PartyType.Organisation,
            OrgNumber = systemUserOrgNumber,
            Name = "Test AS",
        };
        return new SystemUser(
            [Guid.Parse(systemUserId)],
            OrganisationNumber.Parse(systemUserOrgNumber),
            Guid.Parse(systemId),
            "",
            lookupParty: orgNo =>
            {
                Assert.Equal(systemUserOrgNumber, orgNo);
                return Task.FromResult(party);
            },
            getApplicationMetadata: _getApplicationMetadata
        );
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
}
