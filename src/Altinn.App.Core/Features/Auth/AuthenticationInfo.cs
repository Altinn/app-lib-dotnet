using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Constants;
using AltinnCore.Authentication.Utils;
using Authorization.Platform.Authorization.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Auth;

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
        public int UserPartyId { get; }

        /// <summary>
        /// The party the user has selected through party selection
        /// </summary>
        public int SelectedPartyId { get; }

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
        private readonly Func<Task<ApplicationMetadata>> _getApplicationMetadata;

        internal User(
            int userId,
            int userPartyId,
            int authenticationLevel,
            int selectedPartyId,
            string token,
            Func<int, Task<UserProfile?>> getUserProfile,
            Func<int, Task<Party?>> lookupParty,
            Func<int, Task<List<Party>?>> getPartyList,
            Func<int, int, Task<bool?>> validateSelectedParty,
            Func<int, int, Task<IEnumerable<Role>>> getUserRoles,
            Func<Task<ApplicationMetadata>> getApplicationMetadata
        )
            : base(token)
        {
            UserId = userId;
            UserPartyId = userPartyId;
            SelectedPartyId = selectedPartyId;
            AuthenticationLevel = authenticationLevel;
            _getUserProfile = getUserProfile;
            _lookupParty = lookupParty;
            _getPartyList = getPartyList;
            _validateSelectedParty = validateSelectedParty;
            _getUserRoles = getUserRoles;
            _getApplicationMetadata = getApplicationMetadata;
        }

        /// <summary>
        /// Detailed information about a logged in user
        /// </summary>
        /// <param name="UserParty">Party object for the user. This means that the user is currently representing themselves as a person</param>
        /// <param name="SelectedParty">
        ///     Party object for the selected party.
        ///     Selected party and user party will differ when the user has chosed to represent a different entity during party selection (e.g. an organisation)
        /// </param>
        /// <param name="Profile">Users profile</param>
        /// <param name="RepresentsSelf">True if the user represents itself (user party will equal selected party)</param>
        /// <param name="Parties">List of parties the user can represent</param>
        /// <param name="PartiesAllowedToInstantiate">List of parties the user can instantiate</param>
        /// <param name="Roles">List of roles the user has</param>
        /// <param name="CanRepresent">True if the user can represent the selected party. Only set if details were loaded with validateSelectedParty set to true</param>
        public sealed record Details(
            Party UserParty,
            Party SelectedParty,
            UserProfile Profile,
            bool RepresentsSelf,
            IReadOnlyList<Party> Parties,
            IReadOnlyList<Party> PartiesAllowedToInstantiate,
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
            if (userProfile.Party is null)
                throw new InvalidOperationException("Could not get user party from profile");

            var lookupPartyTask =
                SelectedPartyId == userProfile.PartyId
                    ? Task.FromResult((Party?)userProfile.Party)
                    : _lookupParty(SelectedPartyId);
            var partiesTask = _getPartyList(UserId);
            await Task.WhenAll(lookupPartyTask, partiesTask);

            var parties = await partiesTask ?? [];
            if (parties.Count == 0)
                parties.Add(userProfile.Party);

            var selectedParty = await lookupPartyTask;
            if (selectedParty is null)
                throw new InvalidOperationException("Could not load party for selected party ID");

            var representsSelf = SelectedPartyId == userProfile.PartyId;
            bool? canRepresent = null;
            if (validateSelectedParty && !representsSelf)
            {
                // The selected party must either be the profile/default party or a party the user can represent,
                // which can be validated against the user's party list.
                canRepresent = await _validateSelectedParty(UserId, SelectedPartyId);
            }

            var roles = await _getUserRoles(UserId, SelectedPartyId);

            var application = await _getApplicationMetadata();
            var partiesAllowedToInstantiate = InstantiationHelper.FilterPartiesByAllowedPartyTypes(
                parties,
                application.PartyTypesAllowed
            );

            _extra = new Details(
                userProfile.Party,
                selectedParty,
                userProfile,
                representsSelf,
                parties,
                partiesAllowedToInstantiate,
                roles.ToArray(),
                canRepresent
            );
            return _extra;
        }
    }

    /// <summary>
    /// The logged in client is a user (e.g. Altinn portal/IDporten) with auth level 0.
    /// This means that the user has authenticated with a username/password, which can happen using
    /// * Altinn "self registered users"
    /// * IDporten through Ansattporten ("low"), MinID self registered eID
    /// These have limited access to Altinn and can only represent themselves.
    /// </summary>
    public sealed record SelfIdentifiedUser : AuthenticationInfo
    {
        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// User ID
        /// </summary>
        public int UserId { get; }

        /// <summary>
        /// Party ID
        /// </summary>
        public int PartyId { get; }

        private Details? _extra;
        private readonly Func<int, Task<UserProfile?>> _getUserProfile;

        internal SelfIdentifiedUser(
            string username,
            int userId,
            int partyId,
            string token,
            Func<int, Task<UserProfile?>> getUserProfile
        )
            : base(token)
        {
            Username = username;
            UserId = userId;
            PartyId = partyId;
            _getUserProfile = getUserProfile;
        }

        /// <summary>
        /// Authentication level
        /// </summary>
        public static int AuthenticationLevel => 0;

        /// <summary>
        /// Detailed information about a logged in user
        /// </summary>
        public sealed record Details(Party Party, UserProfile Profile, bool RepresentsSelf);

        /// <summary>
        /// Load the details for the current user.
        /// </summary>
        /// <returns></returns>
        public async Task<Details> LoadDetails()
        {
            if (_extra is not null)
                return _extra;

            var userProfile =
                await _getUserProfile(UserId)
                ?? throw new InvalidOperationException("Could not get user profile while getting user context");

            var party = userProfile.Party;
            _extra = new Details(party, userProfile, RepresentsSelf: true);
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
        Func<int, int, Task<IEnumerable<Role>>> getUserRoles,
        Func<Task<ApplicationMetadata>> getApplicationMetadata
    )
    {
        string token = JwtTokenUtil.GetTokenFromContext(httpContext, authCookieName);
        if (string.IsNullOrWhiteSpace(token))
            return new Unauthenticated(token);

        var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
            return new Unauthenticated(token);

        var partyIdClaim = httpContext.User.Claims.FirstOrDefault(claim =>
            claim.Type.Equals(AltinnCoreClaimTypes.PartyID, StringComparison.OrdinalIgnoreCase)
        );

        int? partyId = null;
        if (!string.IsNullOrWhiteSpace(partyIdClaim?.Value))
        {
            // TODO: partyId is only present for org tokens when using virksomhetsbruker
            // which is going away, probably.
            // If we want `Org` to always have party ID, then we need to do a lookup with AltinnPartyClient (register)
            if (!int.TryParse(partyIdClaim.Value, CultureInfo.InvariantCulture, out var partyIdClaimValue))
                throw new InvalidOperationException("Invalid party ID claim value for token");
            partyId = partyIdClaimValue;
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
            if (authorizationDetails is not SystemUserAuthorizationDetailsClaim systemUser)
                throw new InvalidOperationException("Unsupported authorization details claim value for token");

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

        if (partyId is null)
            throw new InvalidOperationException("Missing party ID for user token");

        ParseAuthLevel(authLevelClaim?.Value, out authLevel);
        if (authLevel == 0)
        {
            var usernameClaim = httpContext.User.Claims.FirstOrDefault(claim =>
                claim.Type.Equals(AltinnCoreClaimTypes.UserName, StringComparison.OrdinalIgnoreCase)
            );
            if (string.IsNullOrWhiteSpace(usernameClaim?.Value))
                throw new InvalidOperationException("Missing username claim for self-identified user token");

            return new SelfIdentifiedUser(usernameClaim.Value, userId, partyId.Value, token, getUserProfile);
        }

        int selectedPartyId = partyId.Value;
        if (httpContext.Request.Cookies.TryGetValue(partyCookieName, out var partyCookie) && partyCookie != null)
        {
            if (!int.TryParse(partyCookie, CultureInfo.InvariantCulture, out var cookiePartyIdVal))
                throw new InvalidOperationException("Invalid party ID in cookie: " + partyCookie);

            selectedPartyId = cookiePartyIdVal;
        }

        return new User(
            userId,
            partyId.Value,
            authLevel,
            selectedPartyId,
            token,
            getUserProfile,
            lookupUserParty,
            getPartyList,
            validateSelectedParty,
            getUserRoles,
            getApplicationMetadata
        );
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SystemUserAuthorizationDetailsClaim), typeDiscriminator: "urn:altinn:systemuser")]
    internal record AuthorizationDetailsClaim();

    internal sealed record SystemUserAuthorizationDetailsClaim(
        [property: JsonPropertyName("systemuser_id")] IReadOnlyList<string> SystemUserId,
        [property: JsonPropertyName("system_id")] string SystemId,
        [property: JsonPropertyName("systemuser_org")] SystemUserOrg SystemUserOrg
    ) : AuthorizationDetailsClaim();

    internal sealed record SystemUserOrg(
        [property: JsonPropertyName("authority")] string Authority,
        [property: JsonPropertyName("ID")] string Id
    );
}
