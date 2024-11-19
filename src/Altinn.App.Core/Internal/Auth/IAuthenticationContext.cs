using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Auth;

internal static class AuthenticationContextDI
{
    internal static void AddAuthenticationContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuthenticationContext, AuthenticationContext>();
    }
}

internal abstract record AuthenticationInfo
{
    public string Token { get; }

    private AuthenticationInfo(string token) => Token = token;

    internal sealed record Unauthenticated(string Token) : AuthenticationInfo(Token);

    internal sealed record User(int UserId, int PartyId, string Token) : AuthenticationInfo(Token);

    internal sealed record ServiceOwner(string OrgName, string OrgNo, string Token) : AuthenticationInfo(Token);

    internal sealed record Org(string OrgNo, int PartyId, string Token) : AuthenticationInfo(Token);

    internal sealed record SystemUser(IReadOnlyList<string> SystemUserId, string SystemId, string Token)
        : AuthenticationInfo(Token);

    // internal sealed record App(string Token) : ClientContextData;

    internal static AuthenticationInfo From(HttpContext httpContext, string cookieName)
    {
        string token = JwtTokenUtil.GetTokenFromContext(httpContext, cookieName);
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

        if (!string.IsNullOrWhiteSpace(orgClaim?.Value))
        {
            // In this case the token should have a serviceowner scope,
            // due to the `urn:altinn:org` claim
            if (string.IsNullOrWhiteSpace(orgNoClaim?.Value))
                throw new InvalidOperationException("Missing org number claim for org token");
            if (!string.IsNullOrWhiteSpace(partyIdClaim?.Value))
                throw new InvalidOperationException("Got service owner token");

            // TODO: check if the org is the same as the owner of the app? A flag?

            return new ServiceOwner(orgClaim.Value, orgNoClaim.Value, token);
        }
        else if (!string.IsNullOrWhiteSpace(orgNoClaim?.Value))
        {
            return new Org(orgNoClaim.Value, partyId, token);
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

        return new User(userId, partyId, token);
    }

    private sealed record AuthorizationDetailsClaim([property: JsonPropertyName("type")] string Type);

    private sealed record SystemUserAuthorizationDetailsClaim(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("systemuser_id")] IReadOnlyList<string> SystemUserId,
        [property: JsonPropertyName("system_id")] string SystemId
    );
}

internal interface IAuthenticationContext
{
    AuthenticationInfo Current { get; }
}

internal sealed class AuthenticationContext : IAuthenticationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AppSettings> _appSettings;

    private readonly object _lck = new();

    public AuthenticationContext(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<AppSettings> appSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
    }

    public AuthenticationInfo Current
    {
        get
        {
            // Currently we're coupling this to the HTTP context directly.
            // In the future we might want to run work (e.g. service tasks) in the background,
            // at which point we won't always have a HTTP context available.
            // At that point we probably want to implement something like an `IExecutionContext`, `IExecutionContextAccessor`
            // to decouple ourselves from the ASP.NET request context.
            // TODO: consider removing dependcy on HTTP context
            var httpContext =
                _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context available");

            lock (_lck)
            {
                const string key = "Internal_AltinnAuthenticationInfo";
                if (httpContext.Items.TryGetValue(key, out var authInfoObj))
                {
                    if (authInfoObj is not AuthenticationInfo authInfo)
                        throw new InvalidOperationException("Invalid authentication info object in HTTP context items");
                    return authInfo;
                }
                else
                {
                    var authInfo = AuthenticationInfo.From(httpContext, _appSettings.CurrentValue.RuntimeCookieName);
                    httpContext.Items[key] = authInfo;
                    return authInfo;
                }
            }
        }
    }
}
