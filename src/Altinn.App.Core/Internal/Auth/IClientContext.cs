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

internal static class ClientContextDI
{
    internal static void AddClientContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IClientContext, ClientContext>();
    }
}

internal abstract record ClientContextData(string Token)
{
    internal sealed record Unauthenticated(string Token) : ClientContextData(Token);

    internal sealed record User(int UserId, int PartyId, string Token) : ClientContextData(Token);

    internal sealed record Org(string OrgName, string OrgNo, int PartyId, string Token) : ClientContextData(Token);

    internal sealed record SystemUser(IReadOnlyList<string> SystemUserId, string SystemId, string Token)
        : ClientContextData(Token);

    // internal sealed record App(string Token) : ClientContextData;

    internal static ClientContextData From(HttpContext httpContext, string cookieName)
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
            if (string.IsNullOrWhiteSpace(orgNoClaim?.Value))
                throw new InvalidOperationException("Missing org number claim for org token");

            return new Org(orgClaim.Value, orgNoClaim.Value, partyId, token);
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

internal interface IClientContext
{
    ClientContextData Current { get; }
}

internal sealed class ClientContext : IClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AppSettings> _appSettings;

    public ClientContext(IHttpContextAccessor httpContextAccessor, IOptionsMonitor<AppSettings> appSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
    }

    public ClientContextData Current =>
        ClientContextData.From(
            _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context available"),
            _appSettings.CurrentValue.RuntimeCookieName
        );
}
