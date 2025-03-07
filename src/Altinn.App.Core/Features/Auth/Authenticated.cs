using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Models;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Constants;

namespace Altinn.App.Core.Features.Auth;

/// <summary>
/// Contains information about the current logged in client/user.
/// Represented as a union/type hierarchy to express which information is available.
/// </summary>
public abstract class Authenticated
{
    /// <summary>
    /// Token issuer
    /// </summary>
    public TokenIssuer TokenIssuer { get; }

    /// <summary>
    /// True if the token is exchanged through Altinn Authentication exchange endpoint
    /// </summary>
    public bool TokenIsExchanged { get; }

    /// <summary>
    /// The scopes of the JWT token
    /// </summary>
    public Scopes Scopes { get; }

    /// <summary>
    /// The JWT token
    /// </summary>
    public string Token { get; }

    private Authenticated(TokenIssuer tokenIssuer, bool tokenIsExchanged, Scopes scopes, string token)
    {
        TokenIssuer = tokenIssuer;
        TokenIsExchanged = tokenIsExchanged;
        Scopes = scopes;
        Token = token;
    }

    /// <summary>
    /// Resolves the language for the current authenticated client.
    /// If the client is a user, we will look up the language from the user profile.
    /// In all other cases we default to "nb".
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetLanguage()
    {
        string language = LanguageConst.Nb;

        if (this is not User and not SelfIdentifiedUser)
            return language;

        var profile = this switch
        {
            User user => await user.LookupProfile(),
            SelfIdentifiedUser selfIdentifiedUser => (await selfIdentifiedUser.LoadDetails()).Profile,
            _ => throw new InvalidOperationException($"Unexpected case: {this.GetType().Name}"),
        };

        if (!string.IsNullOrEmpty(profile.ProfileSettingPreference?.Language))
            language = profile.ProfileSettingPreference.Language;

        return language;
    }

    /// <summary>
    /// Type to indicate that the current request is not uathenticated.
    /// </summary>
    public sealed class None : Authenticated
    {
        internal None(TokenIssuer tokenIssuer, bool tokenIsExchanged, Scopes scopes, string token)
            : base(tokenIssuer, tokenIsExchanged, scopes, token) { }
    }

    /// <summary>
    /// The logged in client is a user (e.g. Altinn portal/ID-porten)
    /// </summary>
    public sealed class User : Authenticated
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

        /// <summary>
        /// Method of authentication, e.g. "idporten" or "maskinporten"
        /// </summary>
        public string AuthenticationMethod { get; }

        /// <summary>
        /// True if the user was authenticated through the Altinn portal
        /// </summary>
        public bool InAltinnPortal { get; }

        private Details? _extra;
        private readonly Func<int, Task<UserProfile?>> _getUserProfile;
        private readonly Func<int, Task<Party?>> _lookupParty;
        private readonly Func<int, Task<List<Party>?>> _getPartyList;
        private readonly Func<int, int, Task<bool?>> _validateSelectedParty;
        private readonly ApplicationMetadata _appMetadata;

        internal User(
            int userId,
            int userPartyId,
            int authenticationLevel,
            string authenticationMethod,
            int selectedPartyId,
            bool inAltinnPortal,
            TokenIssuer tokenIssuer,
            bool tokenIsExchanged,
            Scopes scopes,
            string token,
            Func<int, Task<UserProfile?>> getUserProfile,
            Func<int, Task<Party?>> lookupParty,
            Func<int, Task<List<Party>?>> getPartyList,
            Func<int, int, Task<bool?>> validateSelectedParty,
            ApplicationMetadata appMetadata
        )
            : base(tokenIssuer, tokenIsExchanged, scopes, token)
        {
            UserId = userId;
            UserPartyId = userPartyId;
            SelectedPartyId = selectedPartyId;
            AuthenticationLevel = authenticationLevel;
            AuthenticationMethod = authenticationMethod;
            InAltinnPortal = inAltinnPortal;
            _getUserProfile = getUserProfile;
            _lookupParty = lookupParty;
            _getPartyList = getPartyList;
            _validateSelectedParty = validateSelectedParty;
            _appMetadata = appMetadata;
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
        /// <param name="PartiesAllowedToInstantiate">List of parties the user can instantiate as</param>
        /// <param name="CanRepresent">True if the user can represent the selected party. Only set if details were loaded with validateSelectedParty set to true</param>
        public sealed record Details(
            Party UserParty,
            Party SelectedParty,
            UserProfile Profile,
            bool RepresentsSelf,
            IReadOnlyList<Party> Parties,
            IReadOnlyList<Party> PartiesAllowedToInstantiate,
            bool? CanRepresent = null
        )
        {
            /// <summary>
            /// Check if the user can represent a party.
            /// </summary>
            /// <param name="partyId">Party ID</param>
            /// <returns></returns>
            public bool CanRepresentParty(int partyId)
            {
                if (partyId == UserParty.PartyId || partyId == SelectedParty.PartyId)
                    return true;

                var partiesToCheck = new Queue<Party>(Parties);
                while (partiesToCheck.Count > 0)
                {
                    var party = partiesToCheck.Dequeue();
                    if (party.PartyId == partyId)
                        return true;

                    if (party.ChildParties is not null)
                    {
                        foreach (var childParty in party.ChildParties)
                            partiesToCheck.Enqueue(childParty);
                    }
                }

                return false;
            }

            /// <summary>
            /// Checks if the current user can instantiate a specific party by ID.
            /// </summary>
            /// <param name="partyId">Party ID</param>
            /// <returns></returns>
            public bool CanInstantiateAsParty(int partyId)
            {
                var partiesToCheck = new Queue<Party>(PartiesAllowedToInstantiate);
                while (partiesToCheck.Count > 0)
                {
                    var party = partiesToCheck.Dequeue();
                    if (party.PartyId == partyId && !party.OnlyHierarchyElementWithNoAccess)
                        return true;

                    if (party.ChildParties is not null)
                    {
                        foreach (var childParty in party.ChildParties)
                            partiesToCheck.Enqueue(childParty);
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Lookup the party for the selected party ID.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the party couldn't be resolved</exception>
        public async Task<Party> LookupSelectedParty() =>
            _extra?.SelectedParty
            ?? await _lookupParty(SelectedPartyId)
            ?? throw new InvalidOperationException($"Could not load party for selected party ID: {SelectedPartyId}");

        /// <summary>
        /// Lookup the user profile for the current user.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<UserProfile> LookupProfile() =>
            _extra?.Profile
            ?? await _getUserProfile(UserId)
            ?? throw new InvalidOperationException("Could not get user profile while getting user context");

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
                ?? throw new AuthenticationContextException($"Could not get user profile for logged in user: {UserId}");
            if (userProfile.Party is null)
                throw new AuthenticationContextException($"Could not get user party from profile for user: {UserId}");

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
                throw new AuthenticationContextException(
                    $"Could not load party for selected party ID: {SelectedPartyId}"
                );

            var representsSelf = SelectedPartyId == userProfile.PartyId;
            bool? canRepresent = null;
            if (representsSelf)
                canRepresent = true;

            if (validateSelectedParty && !representsSelf)
            {
                // The selected party must either be the profile/default party or a party the user can represent,
                // which can be validated against the user's party list.
                canRepresent = await _validateSelectedParty(UserId, SelectedPartyId);
            }

            var partiesAllowedToInstantiate = InstantiationHelper.FilterPartiesByAllowedPartyTypes(
                parties,
                _appMetadata.PartyTypesAllowed
            );

            _extra = new Details(
                userProfile.Party,
                selectedParty,
                userProfile,
                representsSelf,
                parties,
                partiesAllowedToInstantiate,
                canRepresent
            );
            return _extra;
        }
    }

    /// <summary>
    /// The logged in client is a user (e.g. Altinn portal/ID-porten) with auth level 0.
    /// This means that the user has authenticated with a username/password, which can happen using
    /// * Altinn "self registered users"
    /// * ID-porten through Ansattporten ("low"), MinID self registered eID
    /// These have limited access to Altinn and can only represent themselves.
    /// </summary>
    public sealed class SelfIdentifiedUser : Authenticated
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

        /// <summary>
        /// Method of authentication, e.g. "idporten" or "maskinporten"
        /// </summary>
        public string AuthenticationMethod { get; }

        private Details? _extra;
        private readonly Func<int, Task<UserProfile?>> _getUserProfile;
        private readonly ApplicationMetadata _appMetadata;

        internal SelfIdentifiedUser(
            string username,
            int userId,
            int partyId,
            string authenticationMethod,
            TokenIssuer tokenIssuer,
            bool tokenIsExchanged,
            Scopes scopes,
            string token,
            Func<int, Task<UserProfile?>> getUserProfile,
            ApplicationMetadata appMetadata
        )
            : base(tokenIssuer, tokenIsExchanged, scopes, token)
        {
            Username = username;
            UserId = userId;
            PartyId = partyId;
            AuthenticationMethod = authenticationMethod;
            // Since they are self-identified, they are always 0
            AuthenticationLevel = 0;
            _getUserProfile = getUserProfile;
            _appMetadata = appMetadata;
        }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }

        /// <summary>
        /// Detailed information about a logged in user
        /// </summary>
        public sealed record Details(Party Party, UserProfile Profile, bool RepresentsSelf, bool CanInstantiate);

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
                ?? throw new AuthenticationContextException(
                    $"Could not get user profile for logged in self identified user: {UserId}"
                );

            var party = userProfile.Party;
            var canInstantiate = InstantiationHelper.IsPartyAllowedToInstantiate(party, _appMetadata.PartyTypesAllowed);
            _extra = new Details(party, userProfile, RepresentsSelf: true, canInstantiate);
            return _extra;
        }
    }

    /// <summary>
    /// The logged in client is an organisation (but they have not authenticated as an Altinn service owner).
    /// Authentication has been done through Maskinporten.
    /// </summary>
    public sealed class Org : Authenticated
    {
        /// <summary>
        /// Organisation number
        /// </summary>
        public string OrgNo { get; }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }

        /// <summary>
        /// Method of authentication, e.g. "idporten" or "maskinporten"
        /// </summary>
        public string AuthenticationMethod { get; }

        private readonly Func<string, Task<Party>> _lookupParty;
        private readonly ApplicationMetadata _appMetadata;

        internal Org(
            string orgNo,
            int authenticationLevel,
            string authenticationMethod,
            TokenIssuer tokenIssuer,
            bool tokenIsExchanged,
            Scopes scopes,
            string token,
            Func<string, Task<Party>> lookupParty,
            ApplicationMetadata appMetadata
        )
            : base(tokenIssuer, tokenIsExchanged, scopes, token)
        {
            OrgNo = orgNo;
            AuthenticationLevel = authenticationLevel;
            AuthenticationMethod = authenticationMethod;
            _lookupParty = lookupParty;
            _appMetadata = appMetadata;
        }

        /// <summary>
        /// Detailed information about an organisation
        /// </summary>
        /// <param name="Party">Party of the org</param>
        /// <param name="CanInstantiate">True if the org can instantiate applications</param>
        public sealed record Details(Party Party, bool CanInstantiate);

        /// <summary>
        /// Load the details for the current organisation.
        /// </summary>
        /// <returns>Details</returns>
        public async Task<Details> LoadDetails()
        {
            var party = await _lookupParty(OrgNo);

            var canInstantiate = InstantiationHelper.IsPartyAllowedToInstantiate(party, _appMetadata.PartyTypesAllowed);

            return new Details(party, canInstantiate);
        }
    }

    /// <summary>
    /// The logged in client is an Altinn service owner (i.e. they have the "urn:altinn:org" claim).
    /// The service owner may or may not own the current app.
    /// </summary>
    public sealed class ServiceOwner : Authenticated
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

        /// <summary>
        /// Method of authentication, e.g. "idporten" or "maskinporten"
        /// </summary>
        public string AuthenticationMethod { get; }

        private readonly Func<string, Task<Party>> _lookupParty;

        internal ServiceOwner(
            string name,
            string orgNo,
            int authenticationLevel,
            string authenticationMethod,
            TokenIssuer tokenIssuer,
            bool tokenIsExchanged,
            Scopes scopes,
            string token,
            Func<string, Task<Party>> lookupParty
        )
            : base(tokenIssuer, tokenIsExchanged, scopes, token)
        {
            Name = name;
            OrgNo = orgNo;
            AuthenticationLevel = authenticationLevel;
            AuthenticationMethod = authenticationMethod;
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
    public sealed class SystemUser : Authenticated
    {
        /// <summary>
        /// System user ID
        /// </summary>
        public IReadOnlyList<Guid> SystemUserId { get; }

        /// <summary>
        /// Organisation number of the system user
        /// </summary>
        public OrganisationNumber SystemUserOrgNr { get; }

        /// <summary>
        /// Organisation number of the supplier system
        /// </summary>
        public OrganisationNumber SupplierOrgNr { get; }

        /// <summary>
        /// System ID
        /// </summary>
        public string SystemId { get; }

        /// <summary>
        /// Authentication level
        /// </summary>
        public int AuthenticationLevel { get; }

        /// <summary>
        /// Method of authentication
        /// </summary>
        public string AuthenticationMethod { get; }

        private readonly Func<string, Task<Party>> _lookupParty;
        private readonly ApplicationMetadata _appMetadata;

        internal SystemUser(
            IReadOnlyList<Guid> systemUserId,
            OrganisationNumber systemUserOrgNr,
            OrganisationNumber supplierOrgNr,
            string systemId,
            int? authenticationLevel,
            string? authenticationMethod,
            TokenIssuer tokenIssuer,
            bool tokenIsExchanged,
            Scopes scopes,
            string token,
            Func<string, Task<Party>> lookupParty,
            ApplicationMetadata appMetadata
        )
            : base(tokenIssuer, tokenIsExchanged, scopes, token)
        {
            SystemUserId = systemUserId;
            SystemUserOrgNr = systemUserOrgNr;
            SupplierOrgNr = supplierOrgNr;
            SystemId = systemId;
            // System user tokens can either be raw Maskinporten or exchanged atm.
            // If the token is not exchanged, we don't have these claims and so we default to what altinn-authentication currently does.
            AuthenticationLevel = authenticationLevel ?? 3;
            AuthenticationMethod = authenticationMethod ?? "maskinporten";
            _lookupParty = lookupParty;
            _appMetadata = appMetadata;
        }

        /// <summary>
        /// Detailed information about a system user
        /// </summary>
        /// <param name="Party">Party of the system user</param>
        /// <param name="CanInstantiate">True if the system user can instantiate applications</param>
        public sealed record Details(Party Party, bool CanInstantiate);

        /// <summary>
        /// Load the details for the current system user.
        /// </summary>
        /// <returns>Details</returns>
        public async Task<Details> LoadDetails()
        {
            var party = await _lookupParty(SystemUserOrgNr.Get(OrganisationNumberFormat.Local));

            var canInstantiate = InstantiationHelper.IsPartyAllowedToInstantiate(party, _appMetadata.PartyTypesAllowed);

            return new Details(party, canInstantiate);
        }
    }

    // TODO: app token?
    // public sealed record App(string Token) : Authenticated;

    private static (TokenIssuer Issuer, bool IsExchanged) ResolveIssuer(
        TokenClaim issClaimn,
        TokenClaim authMethodClaim,
        TokenClaim acrClaim,
        bool isInAltinnPortal
    )
    {
        if (!issClaimn.IsValidString(out var iss) & !authMethodClaim.IsValidString(out var authMethod))
            return (TokenIssuer.None, false);

        // A token is exchanged if
        // * issuer is altinn.no (either by verifying iss or that the urn:altinn:authenticatemethod claim is set)
        // * scope does not contain altinn:portal/enduser (this is a special scope used only by altinn-authentication).
        //   This should hold true as long as we know we only get tokens from Altinn Authentication or ID porten/Maskinporten directly (otherwise the scope ownership is unclear)
        var isExchanged =
            (iss?.Contains("altinn.no", StringComparison.OrdinalIgnoreCase) is true || authMethod is not null)
            && !isInAltinnPortal;

        // If we have the special scope, we know the login was done through Altinn portal directly
        // In any other case we want the underlying authentication method (ID-porten, Maskinporten)
        if (isInAltinnPortal)
            return (TokenIssuer.Altinn, isExchanged);

        if (iss is not null)
        {
            // If the issuer is not altinn.no, we know it is not exchanged and we can directly determine the issuer
            if (iss.Contains("studio", StringComparison.OrdinalIgnoreCase))
                return (TokenIssuer.AltinnStudio, isExchanged);
            if (iss.Contains("idporten.no", StringComparison.OrdinalIgnoreCase))
                return (TokenIssuer.IDporten, isExchanged);
            if (iss.Contains("maskinporten.no", StringComparison.OrdinalIgnoreCase))
                return (TokenIssuer.Maskinporten, isExchanged);
        }
        if (authMethod is not null)
        {
            // IdportenTestId is the authmetod when logging into altinn portal with test users, e.g. in a tt02 app
            // though this case should already be handled by the portal/enduser scope check
            if (authMethod.Equals("IdportenTestId", StringComparison.OrdinalIgnoreCase))
                return (TokenIssuer.Altinn, isExchanged);

            if (
                authMethod.Equals("maskinporten", StringComparison.OrdinalIgnoreCase) // From altinn-authentication
                || authMethod.Equals("systemuser", StringComparison.OrdinalIgnoreCase) // From AltinnTestTools
                || authMethod.Equals("virksomhetsbruker", StringComparison.OrdinalIgnoreCase) // From altinn-authentication when using virksomhetsbruker
            )
            {
                Debug.Assert(isExchanged, "When we have authMethod, the token should always be exchanged");
                return (TokenIssuer.Maskinporten, isExchanged);
            }
        }

        // IDportens authenticationlevel equivalent will only be present if the token originates from ID-porten
        // We should already be handling the ID-porten through Altinn portal case (with the scope)
        if (acrClaim.IsValidString(out var acr) && acr.StartsWith("idporten", StringComparison.OrdinalIgnoreCase))
            return (TokenIssuer.IDporten, isExchanged);

        return (TokenIssuer.Unknown, isExchanged);
    }

    internal static Authenticated FromLocalTest(
        string tokenStr,
        ApplicationMetadata appMetadata,
        Func<string?> getSelectedParty,
        Func<int, Task<UserProfile?>> getUserProfile,
        Func<int, Task<Party?>> lookupUserParty,
        Func<string, Task<Party>> lookupOrgParty,
        Func<int, Task<List<Party>?>> getPartyList,
        Func<int, int, Task<bool?>> validateSelectedParty
    )
    {
        if (string.IsNullOrWhiteSpace(tokenStr))
            return new None(TokenIssuer.None, false, Scopes.None, tokenStr);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        TokenClaim authLevelClaim = default;
        TokenClaim orgClaim = default;
        TokenClaim orgNoClaim = default;
        TokenClaim userIdClaim = default;
        TokenClaim usernameClaim = default;
        TokenClaim partyIdClaim = default;
        TokenClaim scopeClaim = default;

        var tokenIssuer = TokenIssuer.Altinn;
        var isExchanged = true;

        foreach (var claim in token.Payload)
        {
            TryAssign(claim, AltinnCoreClaimTypes.AuthenticationLevel, ref authLevelClaim);
            TryAssign(claim, AltinnCoreClaimTypes.Org, ref orgClaim);
            TryAssign(claim, AltinnCoreClaimTypes.OrgNumber, ref orgNoClaim);
            TryAssign(claim, AltinnCoreClaimTypes.PartyID, ref partyIdClaim);
            TryAssign(claim, AltinnCoreClaimTypes.UserId, ref userIdClaim);
            TryAssign(claim, AltinnCoreClaimTypes.UserName, ref usernameClaim);
            TryAssign(claim, JwtClaimTypes.Scope, ref scopeClaim);
        }

        if (!scopeClaim.IsValidString(out var scopeClaimValue))
            throw new AuthenticationContextException("Invalid scope claim value for token");
        var scopes = new Scopes(scopeClaimValue);

        int? partyId = null;
        if (partyIdClaim.Exists)
        {
            if (!partyIdClaim.IsValidInt(out var partyIdClaimValue))
                throw new AuthenticationContextException(
                    $"Invalid party ID claim value for token: {partyIdClaim.Value}"
                );
            partyId = partyIdClaimValue;
        }

        int? authLevel;
        if (orgClaim.Exists)
        {
            if (!orgClaim.IsValidString(out var orgClaimValue))
                throw new AuthenticationContextException(
                    $"Invlaid org claim for service owner token: {orgClaim.Value}"
                );

            // In this case the token should have a serviceowner scope,
            // due to the `urn:altinn:org` claim
            if (!orgNoClaim.IsValidString(out var orgNoClaimValue))
                throw new AuthenticationContextException("Missing or invalid org number claim for service owner token");

            ParseAuthLevel(authLevelClaim, out authLevel);

            return new ServiceOwner(
                orgClaimValue,
                orgNoClaimValue,
                authLevel.Value,
                "localtest",
                tokenIssuer,
                isExchanged,
                scopes,
                tokenStr,
                lookupOrgParty
            );
        }

        if (!userIdClaim.Exists)
            throw new AuthenticationContextException("Missing user ID claim for user token");
        if (!userIdClaim.IsValidString(out var userIdStr))
            throw new AuthenticationContextException(
                $"Invalid user ID claim value for user token: {userIdClaim.Value}"
            );
        if (!int.TryParse(userIdStr, CultureInfo.InvariantCulture, out var userId))
            throw new AuthenticationContextException($"Invalid user ID claim value for user token: {userIdStr}");

        if (partyId is null)
            throw new AuthenticationContextException("Missing party ID for user token");

        ParseAuthLevel(authLevelClaim, out authLevel);
        if (authLevel == 0)
        {
            if (!usernameClaim.IsValidString(out var usernameClaimValue))
                throw new AuthenticationContextException("Missing username claim for self-identified user token");

            return new SelfIdentifiedUser(
                usernameClaimValue,
                userId,
                partyId.Value,
                "localtest",
                tokenIssuer,
                isExchanged,
                scopes,
                tokenStr,
                getUserProfile,
                appMetadata
            );
        }

        int selectedPartyId = partyId.Value;
        if (getSelectedParty() is { } selectedPartyStr)
        {
            if (!int.TryParse(selectedPartyStr, CultureInfo.InvariantCulture, out var selectedParty))
                throw new AuthenticationContextException($"Invalid party ID in cookie: {selectedPartyStr}"); // TODO: maybe not throw?

            selectedPartyId = selectedParty;
        }

        return new User(
            userId,
            partyId.Value,
            authLevel.Value,
            "localtest",
            selectedPartyId,
            true,
            tokenIssuer,
            isExchanged,
            scopes,
            tokenStr,
            getUserProfile,
            lookupUserParty,
            getPartyList,
            validateSelectedParty,
            appMetadata
        );
    }

    internal static Authenticated From(
        string tokenStr,
        bool isAuthenticated,
        ApplicationMetadata appMetadata,
        Func<string?> getSelectedParty,
        Func<int, Task<UserProfile?>> getUserProfile,
        Func<int, Task<Party?>> lookupUserParty,
        Func<string, Task<Party>> lookupOrgParty,
        Func<int, Task<List<Party>?>> getPartyList,
        Func<int, int, Task<bool?>> validateSelectedParty
    )
    {
        if (string.IsNullOrWhiteSpace(tokenStr))
            return new None(TokenIssuer.None, false, Scopes.None, tokenStr);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        TokenClaim issuerClaim = default;
        TokenClaim authLevelClaim = default;
        TokenClaim authMethodClaim = default;
        TokenClaim scopeClaim = default;
        TokenClaim acrClaim = default;
        TokenClaim orgClaim = default;
        TokenClaim orgNoClaim = default;
        TokenClaim partyIdClaim = default;
        TokenClaim authorizationDetailsClaim = default;
        TokenClaim userIdClaim = default;
        TokenClaim usernameClaim = default;
        TokenClaim consumerClaim = default;
        OrgClaim? consumerClaimValue = null;

        foreach (var claim in token.Payload)
        {
            TryAssign(claim, JwtClaimTypes.Issuer, ref issuerClaim);
            TryAssign(claim, AltinnCoreClaimTypes.AuthenticationLevel, ref authLevelClaim);
            TryAssign(claim, AltinnCoreClaimTypes.AuthenticateMethod, ref authMethodClaim);
            TryAssign(claim, JwtClaimTypes.Scope, ref scopeClaim);
            TryAssign(claim, "acr", ref acrClaim);
            TryAssign(claim, AltinnCoreClaimTypes.Org, ref orgClaim);
            TryAssign(claim, AltinnCoreClaimTypes.OrgNumber, ref orgNoClaim);
            TryAssign(claim, AltinnCoreClaimTypes.PartyID, ref partyIdClaim);
            TryAssign(claim, "authorization_details", ref authorizationDetailsClaim);
            TryAssign(claim, AltinnCoreClaimTypes.UserId, ref userIdClaim);
            TryAssign(claim, AltinnCoreClaimTypes.UserName, ref usernameClaim);
            if (TryAssign(claim, "consumer", ref consumerClaim) && consumerClaim.Value is JsonElement consumerJsonClaim)
                consumerClaimValue = JsonSerializer.Deserialize<OrgClaim>(consumerJsonClaim);
        }

        if (!scopeClaim.IsValidString(out var scopeClaimValue))
            throw new AuthenticationContextException("Invalid scope claim value for token");
        var scopes = new Scopes(scopeClaimValue);
        var isInAltinnPortal = scopes.HasScope("altinn:portal/enduser");

        var (tokenIssuer, isExchanged) = ResolveIssuer(issuerClaim, authMethodClaim, acrClaim, isInAltinnPortal);

        if (!isAuthenticated)
            return new None(tokenIssuer, isExchanged, scopes, tokenStr);

        int? partyId = null;
        if (partyIdClaim.Exists)
        {
            if (!partyIdClaim.IsValidInt(out var partyIdClaimValue))
                throw new AuthenticationContextException(
                    $"Invalid party ID claim value for token: {partyIdClaim.Value}"
                );
            partyId = partyIdClaimValue;
        }

        int? authLevel;
        if (authorizationDetailsClaim.Exists)
        {
            if (!authorizationDetailsClaim.IsJson(out var json))
                throw new AuthenticationContextException(
                    $"Invalid authorization details claim value for token: {json}"
                );
            var authorizationDetails = json.Value.ValueKind switch
            {
                JsonValueKind.Object => JsonSerializer.Deserialize<AuthorizationDetailsClaim>(json.Value),
                JsonValueKind.Array when json.Value.GetArrayLength() == 1 => JsonSerializer
                    .Deserialize<AuthorizationDetailsClaim[]>(json.Value)
                    ?[0],
                _ => throw new AuthenticationContextException(
                    "Invalid authorization details claim value for systemuser token: " + authorizationDetailsClaim.Value
                ),
            };
            if (authorizationDetails is null)
                throw new AuthenticationContextException(
                    "Invalid authorization details claim value for systemuser token"
                );
            if (authorizationDetails is not SystemUserAuthorizationDetailsClaim systemUser)
                throw new AuthenticationContextException(
                    $"Unsupported authorization details claim value for systemuser token: {authorizationDetails.GetType().Name}"
                );

            if (systemUser is null)
                throw new AuthenticationContextException(
                    "Invalid system user authorization details claim value for systemuser token"
                );
            if (systemUser.SystemUserId is null || systemUser.SystemUserId.Count == 0)
                throw new AuthenticationContextException("Missing system user ID claim for systemuser token");
            if (string.IsNullOrWhiteSpace(systemUser.SystemId))
                throw new AuthenticationContextException("Missing system ID claim for systemuser token");
            if (systemUser.SystemUserOrg.Authority != "iso6523-actorid-upis")
                throw new AuthenticationContextException(
                    $"Unsupported organisation authority in systemuser token: {systemUser.SystemUserOrg.Authority}"
                );
            if (!OrganisationNumber.TryParse(systemUser.SystemUserOrg.Id, out var orgNr))
                throw new AuthenticationContextException(
                    $"Invalid system user organisation number in system user token: {systemUser.SystemUserOrg.Id}"
                );
            if (!OrganisationNumber.TryParse(consumerClaimValue?.Id, out var supplierOrgNr))
                throw new AuthenticationContextException(
                    $"Invalid organisation number in supplier organisation number claim for system user token: {consumerClaimValue?.Id}"
                );

            return new SystemUser(
                systemUser.SystemUserId,
                orgNr,
                supplierOrgNr,
                systemUser.SystemId,
                authLevelClaim.IsValidInt(out authLevel) ? authLevel : null,
                authMethodClaim.IsValidString(out var authMethodClaimValue) ? authMethodClaimValue : null,
                tokenIssuer,
                isExchanged,
                scopes,
                tokenStr,
                lookupOrgParty,
                appMetadata
            );
        }
        else if (orgClaim.Exists)
        {
            if (!orgClaim.IsValidString(out var orgClaimValue))
                throw new AuthenticationContextException(
                    $"Invalid org claim for service owner token: {orgClaim.Value}"
                );

            if (orgClaimValue == appMetadata.Org)
            {
                // In this case the token should have a serviceowner scope,
                // due to the `urn:altinn:org` claim
                if (!orgNoClaim.IsValidString(out var orgNoClaimValue))
                    throw new AuthenticationContextException("Missing org number claim for service owner token");
                if (!authMethodClaim.IsValidString(out var authMethodClaimValue))
                    throw new AuthenticationContextException(
                        "Missing or invalid authentication method claim for service owner token"
                    );

                ParseAuthLevel(authLevelClaim, out authLevel);

                return new ServiceOwner(
                    orgClaimValue,
                    orgNoClaimValue,
                    authLevel.Value,
                    authMethodClaimValue,
                    tokenIssuer,
                    isExchanged,
                    scopes,
                    tokenStr,
                    lookupOrgParty
                );
            }
            else
            {
                return ParseOrg(
                    in orgNoClaim,
                    in authLevelClaim,
                    in authMethodClaim,
                    tokenIssuer,
                    isExchanged,
                    in scopes,
                    tokenStr,
                    lookupOrgParty,
                    appMetadata
                );
            }
        }
        else if (orgNoClaim.Exists)
        {
            return ParseOrg(
                in orgNoClaim,
                in authLevelClaim,
                in authMethodClaim,
                tokenIssuer,
                isExchanged,
                in scopes,
                tokenStr,
                lookupOrgParty,
                appMetadata
            );
        }

        {
            if (!userIdClaim.Exists)
                throw new AuthenticationContextException("Missing user ID claim for user token");
            if (!userIdClaim.IsValidString(out var userIdStr))
                throw new AuthenticationContextException(
                    $"Invalid user ID claim value for user token: {userIdClaim.Value}"
                );
            if (!int.TryParse(userIdStr, CultureInfo.InvariantCulture, out var userId))
                throw new AuthenticationContextException($"Invalid user ID claim value for user token: {userIdStr}");

            if (partyId is null)
                throw new AuthenticationContextException("Missing party ID for user token");
            if (!authMethodClaim.IsValidString(out var authMethodClaimValue))
                throw new AuthenticationContextException(
                    "Missing or invalid authentication method claim for user token"
                );

            ParseAuthLevel(authLevelClaim, out authLevel);
            if (authLevel == 0)
            {
                if (!usernameClaim.IsValidString(out var usernameClaimValue))
                    throw new AuthenticationContextException("Missing username claim for self-identified user token");

                return new SelfIdentifiedUser(
                    usernameClaimValue,
                    userId,
                    partyId.Value,
                    authMethodClaimValue,
                    tokenIssuer,
                    isExchanged,
                    scopes,
                    tokenStr,
                    getUserProfile,
                    appMetadata
                );
            }

            int selectedPartyId = partyId.Value;
            if (getSelectedParty() is { } selectedPartyStr)
            {
                if (!int.TryParse(selectedPartyStr, CultureInfo.InvariantCulture, out var selectedParty))
                    throw new AuthenticationContextException($"Invalid party ID in cookie: {selectedPartyStr}"); // TODO: maybe not throw?

                selectedPartyId = selectedParty;
            }

            return new User(
                userId,
                partyId.Value,
                authLevel.Value,
                authMethodClaimValue,
                selectedPartyId,
                isInAltinnPortal,
                tokenIssuer,
                isExchanged,
                scopes,
                tokenStr,
                getUserProfile,
                lookupUserParty,
                getPartyList,
                validateSelectedParty,
                appMetadata
            );
        }
    }

    private static Org ParseOrg(
        in TokenClaim orgNoClaim,
        in TokenClaim authLevelClaim,
        in TokenClaim authMethodClaim,
        TokenIssuer tokenIssuer,
        bool isExchanged,
        in Scopes scopes,
        string tokenStr,
        Func<string, Task<Party>> lookupOrgParty,
        ApplicationMetadata appMetadata
    )
    {
        if (!orgNoClaim.IsValidString(out var orgNoClaimValue))
            throw new AuthenticationContextException("Invalid org number claim for org token");
        ParseAuthLevel(authLevelClaim, out var authLevel);
        if (!authMethodClaim.IsValidString(out var authMethodClaimValue))
            throw new AuthenticationContextException("Missing or invalid authentication method claim for org token");

        return new Org(
            orgNoClaimValue,
            authLevel.Value,
            authMethodClaimValue,
            tokenIssuer,
            isExchanged,
            scopes,
            tokenStr,
            lookupOrgParty,
            appMetadata
        );
    }

    private static void ParseAuthLevel(TokenClaim claim, [NotNull] out int? authLevel)
    {
        if (!claim.Exists)
            throw new AuthenticationContextException($"Missing authentication level claim value for token");
        if (!claim.IsValidInt(out var claimValue))
            throw new AuthenticationContextException(
                $"Invalid authentication level claim value for token: {claim.Value}"
            );

        if (claimValue is < 0 or > 4)
            throw new AuthenticationContextException(
                $"Invalid authentication level claim value for token: {claimValue}"
            );

        authLevel = claimValue.Value;
    }

    private readonly record struct TokenClaim(string? Type, object? Value)
    {
        [MemberNotNullWhen(true, nameof(Type))]
        public bool Exists => Type is not null;

        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsValidString([NotNullWhen(true)] out string? str)
        {
            str = null;

            if (Type is not null && Value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                str = stringValue;
                return true;
            }
            return false;
        }

        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsValidInt([NotNullWhen(true)] out int? integer)
        {
            integer = null;

            if (Type is not null && Value is int intValue)
            {
                integer = intValue;
                return true;
            }

            return false;
        }

        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsJson([NotNullWhen(true)] out JsonElement? json)
        {
            json = null;

            if (Type is not null && Value is JsonElement jsonElement)
            {
                json = jsonElement;
                return true;
            }
            return false;
        }
    }

    private static bool TryAssign(KeyValuePair<string, object> claim, string name, ref TokenClaim dest)
    {
        if (claim.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            dest = new TokenClaim(claim.Key, claim.Value);
            return true;
        }
        return false;
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SystemUserAuthorizationDetailsClaim), typeDiscriminator: "urn:altinn:systemuser")]
    internal record AuthorizationDetailsClaim();

    internal sealed record SystemUserAuthorizationDetailsClaim(
        [property: JsonPropertyName("systemuser_id")] IReadOnlyList<Guid> SystemUserId,
        [property: JsonPropertyName("system_id")] string SystemId,
        [property: JsonPropertyName("systemuser_org")] OrgClaim SystemUserOrg
    ) : AuthorizationDetailsClaim();

    internal sealed record OrgClaim(
        [property: JsonPropertyName("authority")] string Authority,
        [property: JsonPropertyName("ID")] string Id
    );
}
