using System.Globalization;
using System.Net.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Auth;

internal static class AuthenticationContextDI
{
    internal static void AddAuthenticationContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuthenticationContext, AuthenticationContext>();
    }
}

public abstract record AuthenticationInfo
{
    public string Token { get; }

    private AuthenticationInfo(string token) => Token = token;

    public sealed record Unauthenticated(string Token) : AuthenticationInfo(Token);

    public sealed record User(
        int UserId,
        int PartyId,
        Party? Reportee,
        Party? Party,
        UserProfile Profile,
        int AuthenticationLevel,
        string Token
    ) : AuthenticationInfo(Token);

    public sealed record ServiceOwner(string OrgName, string OrgNo, int AuthenticationLevel, string Token)
        : AuthenticationInfo(Token);

    public sealed record Org(string OrgNo, int PartyId, int AuthenticationLevel, string Token)
        : AuthenticationInfo(Token);

    public sealed record SystemUser(IReadOnlyList<string> SystemUserId, string SystemId, string Token)
        : AuthenticationInfo(Token);

    // public sealed record App(string Token) : ClientContextData;

    internal static async Task<AuthenticationInfo> From(
        HttpContext httpContext,
        string authCookieName,
        string partyCookieName,
        Func<int, Task<UserProfile?>> getUserProfile,
        Func<int, Task<Party?>> lookupParty
    )
    {
        string token = JwtTokenUtil.GetTokenFromContext(httpContext, authCookieName);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Couldn't extract current client token from context");

        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
            return new Unauthenticated(token);

        var partyIdClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.PartyID, StringComparison.OrdinalIgnoreCase)
        );

        if (string.IsNullOrWhiteSpace(partyIdClaim?.Value))
            throw new InvalidOperationException("Missing party ID claim for token");
        if (!int.TryParse(partyIdClaim.Value, CultureInfo.InvariantCulture, out int partyId))
            throw new InvalidOperationException("Invalid party ID claim value for token");

        var orgClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.Org, StringComparison.OrdinalIgnoreCase)
        );
        var orgNoClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.OrgNumber, StringComparison.OrdinalIgnoreCase)
        );

        var authLevelClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.AuthenticationLevel, StringComparison.OrdinalIgnoreCase)
        );

        int authLevel = -1;
        static void ParseAuthLevel(string? value, out int authLevel)
        {
            if (!int.TryParse(value, CultureInfo.InvariantCulture, out authLevel))
                throw new InvalidOperationException("Missing authentication level claim value for token");

            if (authLevel > 4 || authLevel < 0) // TODO - better validation?
                throw new InvalidOperationException("Invalid authentication level claim value for token");
        }

        if (!string.IsNullOrWhiteSpace(orgClaim?.Value))
        {
            // In this case the token should have a serviceowner scope,
            // due to the `urn:altinn:org` claim
            if (string.IsNullOrWhiteSpace(orgNoClaim?.Value))
                throw new InvalidOperationException("Missing org number claim for service owner token");
            if (!string.IsNullOrWhiteSpace(partyIdClaim?.Value))
                throw new InvalidOperationException("Got service owner token");

            ParseAuthLevel(authLevelClaim?.Value, out authLevel);

            // TODO: check if the org is the same as the owner of the app? A flag?

            return new ServiceOwner(orgClaim.Value, orgNoClaim.Value, authLevel, token);
        }
        else if (!string.IsNullOrWhiteSpace(orgNoClaim?.Value))
        {
            ParseAuthLevel(authLevelClaim?.Value, out authLevel);

            return new Org(orgNoClaim.Value, partyId, authLevel, token);
        }

        var authorizationDetailsClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals("authorization_details", StringComparison.OrdinalIgnoreCase)
        );
        if (!string.IsNullOrWhiteSpace(authorizationDetailsClaim?.Value))
        {
            var authorizationDetails = JsonSerializer.Deserialize<AuthorizationDetailsClaim>(
                authorizationDetailsClaim.Value
            );
            if (authorizationDetails is null)
                throw new InvalidOperationException("Invalid authorization details claim value for token");
            if (authorizationDetails.Type != "urn:altinn:systemuser")
                throw new InvalidOperationException(
                    "Receieved authorization details claim for unsupported client/user type"
                );

            var systemUser = JsonSerializer.Deserialize<SystemUserAuthorizationDetailsClaim>(
                authorizationDetailsClaim.Value
            );
            if (systemUser is null)
                throw new InvalidOperationException("Invalid system user authorization details claim value for token");
            if (systemUser.SystemUserId is null || systemUser.SystemUserId.Count == 0)
                throw new InvalidOperationException("Missing system user ID claim for system user token");
            if (string.IsNullOrWhiteSpace(systemUser.SystemId))
                throw new InvalidOperationException("Missing system ID claim for system user token");

            return new SystemUser(systemUser.SystemUserId, systemUser.SystemId, token);
        }

        var userIdClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.UserId, StringComparison.OrdinalIgnoreCase)
        );
        if (string.IsNullOrWhiteSpace(userIdClaim?.Value))
            throw new InvalidOperationException("Missing user ID claim for user token");
        if (!int.TryParse(userIdClaim.Value, CultureInfo.InvariantCulture, out int userId))
            throw new InvalidOperationException("Invalid user ID claim value for user token");

        var userProfile =
            await getUserProfile(userId)
            ?? throw new InvalidOperationException("Could not get user profile while getting user context");

        if (httpContext.Request.Cookies.TryGetValue(partyCookieName, out var partyCookie) && partyCookie != null)
        {
            if (!int.TryParse(partyCookie, CultureInfo.InvariantCulture, out var cookiePartyId))
                throw new InvalidOperationException("Invalid party ID in cookie: " + partyCookie);

            partyId = cookiePartyId;
        }

        ParseAuthLevel(authLevelClaim?.Value, out authLevel);

        var reportee = partyId == userProfile.PartyId ? userProfile.Party : await lookupParty(partyId);
        return new User(userId, partyId, reportee, userProfile.Party, userProfile, authLevel, token);
    }

    private sealed record AuthorizationDetailsClaim([property: JsonPropertyName("type")] string Type);

    private sealed record SystemUserAuthorizationDetailsClaim(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("systemuser_id")] IReadOnlyList<string> SystemUserId,
        [property: JsonPropertyName("system_id")] string SystemId
    );
}

public interface IAuthenticationContext
{
    AuthenticationInfo Current { get; }
}

internal sealed class AuthenticationContext : IAuthenticationContext
{
    private const string ItemsKey = "Internal_AltinnAuthenticationInfo";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly IOptionsMonitor<GeneralSettings> _generalSettings;
    private readonly IProfileClient _profileClient;
    private readonly IAltinnPartyClient _altinnPartyClient;

    public AuthenticationContext(
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AppSettings> appSettings,
        IOptionsMonitor<GeneralSettings> generalSettings,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClient
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
        _generalSettings = generalSettings;
        _profileClient = profileClient;
        _altinnPartyClient = altinnPartyClient;
    }

    // Currently we're coupling this to the HTTP context directly.
    // In the future we might want to run work (e.g. service tasks) in the background,
    // at which point we won't always have a HTTP context available.
    // At that point we probably want to implement something like an `IExecutionContext`, `IExecutionContextAccessor`
    // to decouple ourselves from the ASP.NET request context.
    // TODO: consider removing dependcy on HTTP context
    private HttpContext _httpContext =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context available");

    internal async Task ResolveCurrent()
    {
        var httpContext = _httpContext;
        var authInfo = await AuthenticationInfo.From(
            httpContext,
            _appSettings.CurrentValue.RuntimeCookieName,
            _generalSettings.CurrentValue.GetAltinnPartyCookieName,
            _profileClient.GetUserProfile,
            _altinnPartyClient.GetParty
        );
        httpContext.Items[ItemsKey] = authInfo;
    }

    public AuthenticationInfo Current
    {
        get
        {
            var httpContext = _httpContext;

            if (httpContext.Items.TryGetValue(ItemsKey, out var authInfoObj))
                throw new InvalidOperationException("Authentication info was not populated");
            if (authInfoObj is not AuthenticationInfo authInfo)
                throw new InvalidOperationException("Invalid authentication info object in HTTP context items");
            return authInfo;
        }
    }
}
