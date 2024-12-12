using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.Utils;
using Authorization.Platform.Authorization.Models;
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

/// <summary>
/// Contains information about the current logged in client/user.
/// Represented as a union/type hierarchy to express which information is available.
/// </summary>
public abstract record AuthenticationInfo
{
    /// <summary>
    /// The JWT token.
    /// </summary>
    public string Token { get; }

    private AuthenticationInfo(string token) => Token = token;

    /// <summary>
    /// Type to indicate that the current request is not uathenticated.
    /// </summary>
    /// <param name="Token"></param>
    public sealed record Unauthenticated(string Token) : AuthenticationInfo(Token);

    /// <summary>
    /// The logged in client is a user (e.g. Altinn portal/IDporten)
    /// </summary>
    public sealed record User : AuthenticationInfo
    {
        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; }

        /// <summary>
        /// Party ID
        /// </summary>
        public int PartyId { get; }

        /// <summary>
        /// Party ID from party ID selection cookie
        /// </summary>
        public int? CookiePartyId { get; }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }
        private Details? _extra;
        private readonly Func<int, Task<UserProfile?>> _getUserProfile;
        private readonly Func<int, Task<Party?>> _lookupParty;
        private readonly Func<int, Task<List<Party>?>> _getPartyList;
        private readonly Func<int, int, Task<bool?>> _validateSelectedParty;
        private readonly Func<int, int, Task<IEnumerable<Role>>> _getUserRoles;

        internal User(
            int userId,
            int partyId,
            int authenticationLevel,
            int? cookiePartyId,
            string token,
            Func<int, Task<UserProfile?>> getUserProfile,
            Func<int, Task<Party?>> lookupParty,
            Func<int, Task<List<Party>?>> getPartyList,
            Func<int, int, Task<bool?>> validateSelectedParty,
            Func<int, int, Task<IEnumerable<Role>>> getUserRoles
        )
            : base(token)
        {
            UserId = userId;
            PartyId = partyId;
            CookiePartyId = cookiePartyId;
            AuthenticationLevel = authenticationLevel;
            _getUserProfile = getUserProfile;
            _lookupParty = lookupParty;
            _getPartyList = getPartyList;
            _validateSelectedParty = validateSelectedParty;
            _getUserRoles = getUserRoles;
        }

        /// <summary>
        /// Detailed information about a logged in user
        /// </summary>
        /// <param name="Reportee">Party objectd for the selected party ID</param>
        /// <param name="Profile">Users profile</param>
        /// <param name="RepresentsSelf">True if the user represents itself</param>
        /// <param name="Parties">List of parties the user can represent</param>
        /// <param name="Roles">List of roles the user has</param>
        /// <param name="CanRepresent">True if the user can represent the selected party. Only set if details were loaded with validateSelectedParty set to true</param>
        public sealed record Details(
            Party Reportee,
            UserProfile Profile,
            bool RepresentsSelf,
            IReadOnlyList<Party> Parties,
            IReadOnlyList<Role> Roles,
            bool? CanRepresent = null
        );

        /// <summary>
        /// Load the details for the current user.
        /// </summary>
        /// <param name="validateSelectedParty">If true, will verify that the logged in user has access to the selected party</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the user doesn't have access to the selected party</exception>
        public async Task<Details> LoadDetails(bool validateSelectedParty = false)
        {
            if (_extra is not null)
                return _extra;

            var userProfile =
                await _getUserProfile(UserId)
                ?? throw new InvalidOperationException("Could not get user profile while getting user context");

            var lookupPartyTask =
                PartyId == userProfile.PartyId ? Task.FromResult((Party?)userProfile.Party) : _lookupParty(PartyId);
            var partiesTask = _getPartyList(UserId);
            await Task.WhenAll(lookupPartyTask, partiesTask);

            var parties = await partiesTask ?? [];
            if (parties.Count == 0)
                parties.Add(userProfile.Party);

            var reportee = await lookupPartyTask;
            if (reportee is null)
                throw new InvalidOperationException("Could not load party for selected party ID");

            var representsSelf = userProfile.Party is null || PartyId != userProfile.Party.PartyId;
            bool? canRepresent = null;
            if (validateSelectedParty && !representsSelf)
            {
                // The selected party must either be the profile/default party or a party the user can represent,
                // which can be validated against the user's party list.
                canRepresent = await _validateSelectedParty(UserId, PartyId);
            }

            var roles = await _getUserRoles(UserId, PartyId);

            _extra = new Details(reportee, userProfile, representsSelf, parties, roles.ToArray(), canRepresent);
            return _extra;
        }
    }

    /// <summary>
    /// The logged in client is an organisation (but they have not authenticated as an Altinn service owner).
    /// Authentication has been done through Maskinporten.
    /// </summary>
    public sealed record Org : AuthenticationInfo
    {
        /// <summary>
        /// Organisation number
        /// </summary>
        public string OrgNo { get; }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }

        private readonly Func<string, Task<Party>> _lookupParty;

        internal Org(string orgNo, int authenticationLevel, string token, Func<string, Task<Party>> lookupParty)
            : base(token)
        {
            OrgNo = orgNo;
            AuthenticationLevel = authenticationLevel;
            _lookupParty = lookupParty;
        }

        /// <summary>
        /// Detailed information about an organisation
        /// </summary>
        /// <param name="Party">Party of the org</param>
        public sealed record Details(Party Party);

        /// <summary>
        /// Load the details for the current organisation.
        /// </summary>
        /// <returns>Details</returns>
        public async Task<Details> LoadDetails()
        {
            var party = await _lookupParty(OrgNo);
            return new Details(party);
        }
    }

    /// <summary>
    /// The logged in client is an Altinn service owner (i.e. they have the "urn:altinn:org" claim).
    /// The service owner may or may not own the current app.
    /// </summary>
    public sealed record ServiceOwner : AuthenticationInfo
    {
        /// <summary>
        /// Organisation/service owner name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Organisation number
        /// </summary>
        public string OrgNo { get; }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }

        private readonly Func<string, Task<Party>> _lookupParty;

        internal ServiceOwner(
            string name,
            string orgNo,
            int authenticationLevel,
            string token,
            Func<string, Task<Party>> lookupParty
        )
            : base(token)
        {
            Name = name;
            OrgNo = orgNo;
            AuthenticationLevel = authenticationLevel;
            _lookupParty = lookupParty;
        }

        /// <summary>
        /// Detailed information about a service owner
        /// </summary>
        /// <param name="Party">Party of the service owner</param>
        public sealed record Details(Party Party);

        /// <summary>
        /// Load the details for the current service owner.
        /// </summary>
        /// <returns>Details</returns>
        public async Task<Details> LoadDetails()
        {
            var party = await _lookupParty(OrgNo);
            return new Details(party);
        }
    }

    /// <summary>
    /// The logged in client is a system user.
    /// System users authenticate through Maskinporten.
    /// The caller is the system, which impersonates the system user (which represents the organisation/owner of the user).
    /// </summary>
    public sealed record SystemUser : AuthenticationInfo
    {
        /// <summary>
        /// System user ID
        /// </summary>
        public IReadOnlyList<string> SystemUserId { get; }

        /// <summary>
        /// Organisation number of the system user
        /// </summary>
        public OrganisationNumber SystemUserOrgNr { get; }

        /// <summary>
        /// System ID
        /// </summary>
        public string SystemId { get; }

        private readonly Func<string, Task<Party>> _lookupParty;

        internal SystemUser(
            IReadOnlyList<string> systemUserId,
            OrganisationNumber systemUserOrgNr,
            string systemId,
            string token,
            Func<string, Task<Party>> lookupParty
        )
            : base(token)
        {
            SystemUserId = systemUserId;
            SystemUserOrgNr = systemUserOrgNr;
            SystemId = systemId;
            _lookupParty = lookupParty;
        }

        /// <summary>
        /// Detailed information about a system user
        /// </summary>
        /// <param name="Party">Party of the system user</param>
        public sealed record Details(Party Party);

        /// <summary>
        /// Load the details for the current system user.
        /// </summary>
        /// <returns>Details</returns>
        public async Task<Details> LoadDetails()
        {
            var party = await _lookupParty(SystemUserOrgNr.Get(OrganisationNumberFormat.Local));
            return new Details(party);
        }
    }

    // TODO: app token?
    // public sealed record App(string Token) : AuthenticationInfo;

    internal static AuthenticationInfo From(
        HttpContext httpContext,
        string authCookieName,
        string partyCookieName,
        Func<int, Task<UserProfile?>> getUserProfile,
        Func<int, Task<Party?>> lookupUserParty,
        Func<string, Task<Party>> lookupOrgParty,
        Func<int, Task<List<Party>?>> getPartyList,
        Func<int, int, Task<bool?>> validateSelectedParty,
        Func<int, int, Task<IEnumerable<Role>>> getUserRoles
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

        int? selectedPartyId = null;
        if (!string.IsNullOrWhiteSpace(partyIdClaim?.Value))
        {
            // TODO: partyId is only present for org tokens when using virksomhetsbruker
            // which is going away, probably.
            // If we want `Org` to always have party ID, then we need to do a lookup with AltinnPartyClient (register)
            if (!int.TryParse(partyIdClaim.Value, CultureInfo.InvariantCulture, out var partyIdClaimValue))
                throw new InvalidOperationException("Invalid party ID claim value for token");
            selectedPartyId = partyIdClaimValue;
        }

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

            return new ServiceOwner(orgClaim.Value, orgNoClaim.Value, authLevel, token, lookupOrgParty);
        }
        else if (!string.IsNullOrWhiteSpace(orgNoClaim?.Value))
        {
            ParseAuthLevel(authLevelClaim?.Value, out authLevel);

            return new Org(orgNoClaim.Value, authLevel, token, lookupOrgParty);
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
            if (systemUser.SystemUserOrg.Authority != "iso6523-actorid-upis")
                throw new InvalidOperationException("Unsupported organisation authority in system user token");
            if (!OrganisationNumber.TryParse(systemUser.SystemUserOrg.Id, out var orgNr))
                throw new InvalidOperationException("Invalid organisation number in system user token");

            return new SystemUser(systemUser.SystemUserId, orgNr, systemUser.SystemId, token, lookupOrgParty);
        }

        var userIdClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.UserId, StringComparison.OrdinalIgnoreCase)
        );
        if (string.IsNullOrWhiteSpace(userIdClaim?.Value))
            throw new InvalidOperationException("Missing user ID claim for user token");
        if (!int.TryParse(userIdClaim.Value, CultureInfo.InvariantCulture, out int userId))
            throw new InvalidOperationException("Invalid user ID claim value for user token");

        int? cookiePartyId = null;
        if (httpContext.Request.Cookies.TryGetValue(partyCookieName, out var partyCookie) && partyCookie != null)
        {
            if (!int.TryParse(partyCookie, CultureInfo.InvariantCulture, out var cookiePartyIdVal))
                throw new InvalidOperationException("Invalid party ID in cookie: " + partyCookie);

            cookiePartyId = cookiePartyIdVal;
            selectedPartyId = cookiePartyId;
        }

        ParseAuthLevel(authLevelClaim?.Value, out authLevel);

        if (selectedPartyId is null)
            throw new InvalidOperationException("Missing party ID for user token");

        return new User(
            userId,
            selectedPartyId.Value,
            authLevel,
            cookiePartyId,
            token,
            getUserProfile,
            lookupUserParty,
            getPartyList,
            validateSelectedParty,
            getUserRoles
        );
    }

    private sealed record AuthorizationDetailsClaim([property: JsonPropertyName("type")] string Type);

    private sealed record SystemUserAuthorizationDetailsClaim(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("systemuser_id")] IReadOnlyList<string> SystemUserId,
        [property: JsonPropertyName("system_id")] string SystemId,
        [property: JsonPropertyName("systemuser_org")] SystemUserOrg SystemUserOrg
    );

    private sealed record SystemUserOrg(
        [property: JsonPropertyName("authority")] string Authority,
        [property: JsonPropertyName("ID")] string Id
    );
}

/// <summary>
/// Provides access to the current authentication context.
/// </summary>
internal interface IAuthenticationContext
{
    /// <summary>
    /// The current authentication info.
    /// </summary>
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
    private readonly IAuthorizationClient _authorizationClient;

    public AuthenticationContext(
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AppSettings> appSettings,
        IOptionsMonitor<GeneralSettings> generalSettings,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClient,
        IAuthorizationClient authorizationClient
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
        _generalSettings = generalSettings;
        _profileClient = profileClient;
        _altinnPartyClient = altinnPartyClient;
        _authorizationClient = authorizationClient;
    }

    // Currently we're coupling this to the HTTP context directly.
    // In the future we might want to run work (e.g. service tasks) in the background,
    // at which point we won't always have a HTTP context available.
    // At that point we probably want to implement something like an `IExecutionContext`, `IExecutionContextAccessor`
    // to decouple ourselves from the ASP.NET request context.
    // TODO: consider removing dependcy on HTTP context
    private HttpContext _httpContext =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context available");

    internal void ResolveCurrent()
    {
        var httpContext = _httpContext;
        var authInfo = AuthenticationInfo.From(
            httpContext,
            _appSettings.CurrentValue.RuntimeCookieName,
            _generalSettings.CurrentValue.GetAltinnPartyCookieName,
            _profileClient.GetUserProfile,
            _altinnPartyClient.GetParty,
            (string orgNr) => _altinnPartyClient.LookupParty(new PartyLookup { OrgNo = orgNr }),
            _authorizationClient.GetPartyList,
            _authorizationClient.ValidateSelectedParty,
            _authorizationClient.GetUserRoles
        );
        httpContext.Items[ItemsKey] = authInfo;
    }

    public AuthenticationInfo Current
    {
        get
        {
            var httpContext = _httpContext;

            if (!httpContext.Items.TryGetValue(ItemsKey, out var authInfoObj))
                throw new InvalidOperationException("Authentication info was not populated");
            if (authInfoObj is not AuthenticationInfo authInfo)
                throw new InvalidOperationException("Invalid authentication info object in HTTP context items");
            return authInfo;
        }
    }
}
