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

public sealed record TestJwtToken(AuthenticationTypes Type, int PartyId, string Token, Authenticated Auth)
{
    public override string ToString() => $"{Type}={PartyId}";
}

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
    internal const string DefaultSystemUserOrgNumber = "310702641";
    internal const string DefaultSystemUserSupplierOrgNumber = "991825827";

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

    public static None GetNoneAuthentication()
    {
        return new None(TokenIssuer.None, false, Scopes.None, "None");
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
        int authLevel = DefaultUserAuthenticationLevel
    )
    {
        // Returns a principal that looks like a token issed in tt02 Altinn portal using TestID
        string iss = "https://platform.tt02.altinn.no/authentication/api/v1/openid/";

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, $"user-{userId}-{partyId}", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, iss),
            new(AltinnCoreClaimTypes.AuthenticateMethod, "BankID", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticationLevel, authLevel.ToString(), ClaimValueTypes.Integer32, iss),
            new("jti", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
            new(JwtClaimTypes.Scope, "altinn:portal/enduser", ClaimValueTypes.String, iss),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
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
            "idporten",
            userPartyId,
            inAltinnPortal: true,
            tokenIssuer: TokenIssuer.Altinn,
            tokenIsExchanged: false,
            new Scopes("altinn:portal/enduser"),
            token: "",
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
            appMetadata: NewApplicationMetadata()
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
        // Returns a principal that looks like a token issed in tt02 Altinn portal using the
        // "Logg inn uten fødselsnummer/D-nummber" login method
        string iss = "https://platform.tt02.altinn.no/authentication/api/v1/openid/";

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, $"user-{userId}-{partyId}", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.UserId, userId.ToString(), ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.UserName, username, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.PartyID, partyId.ToString(), ClaimValueTypes.Integer32, iss),
            new(AltinnCoreClaimTypes.AuthenticateMethod, "Mock", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticationLevel, "0", ClaimValueTypes.Integer32, iss),
            new("jti", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
            new(JwtClaimTypes.Scope, "altinn:portal/enduser", ClaimValueTypes.String, iss),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
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
            "idporten",
            tokenIssuer: TokenIssuer.Altinn,
            tokenIsExchanged: false,
            new Scopes("altinn:portal/enduser"),
            token: "",
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
            appMetadata: NewApplicationMetadata()
        );
    }

    public static ClaimsPrincipal GetOrgPrincipal(string orgNumber = DefaultOrgNumber, string scope = DefaultOrgScope)
    {
        // Returns a principal that looks like a token issued by Maskinporten and exchanged to Altinn token in tt02
        // This is not a service owner token, so there should be no service owner scope
        string iss = "https://platform.tt02.altinn.no/authentication/api/v1/openid/";

        var scopes = new Scopes(scope);
        if (scopes.HasScopeWithPrefix("altinn:serviceowner/"))
            throw new InvalidOperationException("Org token cannot have serviceowner scopes");

        var consumer = JsonSerializer.Serialize(
            new OrgClaim(
                "iso6523-actorid-upis",
                OrganisationNumber.Parse(orgNumber).Get(OrganisationNumberFormat.International)
            )
        );
        Claim[] claims =
        [
            new(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, iss),
            new("token_type", "Bearer", ClaimValueTypes.String, iss),
            new("client_id", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
            new("consumer", consumer, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.OrgNumber, orgNumber, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticateMethod, "maskinporten", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, iss),
            new(JwtClaimTypes.Issuer, iss, ClaimValueTypes.String, iss),
            new("jti", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
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
            tokenIssuer: TokenIssuer.Maskinporten,
            tokenIsExchanged: true,
            new Scopes("altinn:instances.read altinn:instances.write"),
            token: "",
            lookupParty: orgNo =>
            {
                Assert.Equal(orgNumber, orgNo);
                return Task.FromResult(party);
            },
            appMetadata: NewApplicationMetadata()
        );
    }

    public static ClaimsPrincipal GetServiceOwnerPrincipal(
        string orgNumber = DefaultOrgNumber,
        string scope = DefaultServiceOwnerScope,
        string org = DefaultOrg
    )
    {
        // Returns a principal that looks like a token issued by Maskinporten and exchanged to Altinn token in tt02
        // This is a service owner token, so there should be atleast 1 service owner scope
        string iss = "https://platform.tt02.altinn.no/authentication/api/v1/openid/";

        var scopes = new Scopes(scope);
        if (!scopes.HasScopeWithPrefix("altinn:serviceowner/"))
            throw new InvalidOperationException("Service owner token must have serviceowner scopes");

        var consumer = JsonSerializer.Serialize(
            new OrgClaim(
                "iso6523-actorid-upis",
                OrganisationNumber.Parse(orgNumber).Get(OrganisationNumberFormat.International)
            )
        );
        Claim[] claims =
        [
            new(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, iss),
            new("token_type", "Bearer", ClaimValueTypes.String, iss),
            new("client_id", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
            new("consumer", consumer, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.Org, org, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.OrgNumber, orgNumber, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticateMethod, "maskinporten", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, iss),
            new(JwtClaimTypes.Issuer, iss, ClaimValueTypes.String, iss),
            new("jti", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
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
            tokenIssuer: TokenIssuer.Maskinporten,
            tokenIsExchanged: true,
            new Scopes("altinn:serviceowner/instances.read altinn:serviceowner/instances.write"),
            token: "",
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
        string systemUserOrgNumber = DefaultSystemUserOrgNumber,
        string supplierOrgNumber = DefaultSystemUserSupplierOrgNumber,
        string scope = DefaultOrgScope
    )
    {
        // Returns a principal that looks like a token issued by Maskinporten and exchanged to Altinn token in tt02
        // This is a service owner token, so there should be atleast 1 service owner scope
        string iss = "https://platform.tt02.altinn.no/authentication/api/v1/openid/";

        var scopes = new Scopes(scope);
        if (scopes.HasScopeWithPrefix("altinn:serviceowner/"))
            throw new InvalidOperationException("System user tokens cannot have serviceowner scopes");

        AuthorizationDetailsClaim details = new SystemUserAuthorizationDetailsClaim(
            [Guid.Parse(systemUserId)],
            systemId,
            new OrgClaim(
                "iso6523-actorid-upis",
                OrganisationNumber.Parse(systemUserOrgNumber).Get(OrganisationNumberFormat.International)
            )
        );
        var consumer = JsonSerializer.Serialize(
            new OrgClaim(
                "iso6523-actorid-upis",
                OrganisationNumber.Parse(supplierOrgNumber).Get(OrganisationNumberFormat.International)
            )
        );
        List<Claim> claims =
        [
            new("authorization_details", JsonSerializer.Serialize(details), ClaimValueTypes.String, iss),
            new(JwtClaimTypes.Scope, scope, ClaimValueTypes.String, iss),
            new("token_type", "Bearer", ClaimValueTypes.String, iss),
            new("client_id", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
            new("consumer", consumer, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.OrgNumber, supplierOrgNumber, ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticateMethod, "maskinporten", ClaimValueTypes.String, iss),
            new(AltinnCoreClaimTypes.AuthenticationLevel, "3", ClaimValueTypes.Integer32, iss),
            new(JwtClaimTypes.Issuer, iss, ClaimValueTypes.String, iss),
            new("jti", Guid.NewGuid().ToString(), ClaimValueTypes.String, iss),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
    }

    public static string GetSystemUserToken(
        string systemId = DefaultSystemId,
        string systemUserId = DefaultSystemUserId,
        string systemUserOrgNumber = DefaultSystemUserOrgNumber,
        string supplierOrgNumber = DefaultSystemUserSupplierOrgNumber,
        string scope = DefaultOrgScope,
        TimeSpan? expiry = null,
        TimeProvider? timeProvider = null
    )
    {
        ClaimsPrincipal principal = GetSystemUserPrincipal(
            systemId,
            systemUserId,
            systemUserOrgNumber,
            supplierOrgNumber,
            scope
        );
        return JwtTokenMock.GenerateToken(principal, expiry ?? TimeSpan.FromMinutes(2), timeProvider);
    }

    public static SystemUser GetSystemUserAuthentication(
        string systemId = DefaultSystemId,
        string systemUserId = DefaultSystemUserId,
        string systemUserOrgNumber = DefaultSystemUserOrgNumber,
        string supplierOrgNumber = DefaultSystemUserSupplierOrgNumber,
        int partyId = DefaultOrgPartyId,
        bool exchanged = true
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
            OrganisationNumber.Parse(supplierOrgNumber),
            systemId,
            3,
            "maskinporten",
            tokenIssuer: TokenIssuer.Maskinporten,
            tokenIsExchanged: exchanged,
            new Scopes("altinn:instances.read altinn:instances.write"),
            token: "",
            lookupParty: orgNo =>
            {
                Assert.Equal(systemUserOrgNumber, orgNo);
                return Task.FromResult(party);
            },
            appMetadata: NewApplicationMetadata()
        );
    }

    public static ApplicationMetadata NewApplicationMetadata(string org = "ttd")
    {
        return new ApplicationMetadata($"{org}/app")
        {
            Org = org,
            PartyTypesAllowed = new PartyTypesAllowed()
            {
                BankruptcyEstate = true,
                Organisation = true,
                Person = true,
                SubUnit = true,
            },
        };
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
