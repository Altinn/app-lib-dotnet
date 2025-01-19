using System.Globalization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Handles party related operations
/// </summary>
[Authorize]
[ApiController]
public class PartiesController : ControllerBase
{
    private readonly IAuthorizationClient _authorizationClient;
    private readonly UserHelper _userHelper;
    private readonly IProfileClient _profileClient;
    private readonly GeneralSettings _settings;
    private readonly IAppMetadata _appMetadata;
    private readonly IAuthenticationContext _authenticationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartiesController"/> class
    /// </summary>
    public PartiesController(
        IAuthorizationClient authorizationClient,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClientClient,
        IOptions<GeneralSettings> settings,
        IAppMetadata appMetadata,
        IAuthenticationContext authenticationContext
    )
    {
        _authorizationClient = authorizationClient;
        _userHelper = new UserHelper(profileClient, altinnPartyClientClient, settings);
        _profileClient = profileClient;
        _settings = settings.Value;
        _appMetadata = appMetadata;
        _authenticationContext = authenticationContext;
    }

    /// <summary>
    /// Gets the list of parties the user can represent
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation.</param>
    /// <param name="allowedToInstantiateFilter">when set to true returns parties that are allowed to instantiate</param>
    /// <returns>parties</returns>
    [Authorize]
    [HttpGet("{org}/{app}/api/v1/parties")]
    public async Task<IActionResult> Get(string org, string app, bool allowedToInstantiateFilter = false)
    {
        var context = _authenticationContext.Current;
        switch (context)
        {
            case Authenticated.None:
                return Unauthorized();
            case Authenticated.User user:
            {
                var details = await user.LoadDetails(validateSelectedParty: false);
                return allowedToInstantiateFilter ? Ok(details.PartiesAllowedToInstantiate) : Ok(details.Parties);
            }
            case Authenticated.SelfIdentifiedUser selfIdentified:
            {
                var details = await selfIdentified.LoadDetails();
                IReadOnlyList<Party> parties = [details.Party];
                return Ok(parties);
            }
            case Authenticated.Org orgInfo:
            {
                var details = await orgInfo.LoadDetails();
                IReadOnlyList<Party> parties = [details.Party];
                return Ok(parties);
            }
            case Authenticated.ServiceOwner serviceOwner:
            {
                var details = await serviceOwner.LoadDetails();
                IReadOnlyList<Party> parties = [details.Party];
                return Ok(parties);
            }
            case Authenticated.SystemUser su:
            {
                var details = await su.LoadDetails();
                IReadOnlyList<Party> parties = [details.Party];
                return Ok(parties);
            }
            default:
                throw new Exception($"Unexpected authentication context: {context.GetType().Name}");
        }
    }

    /// <summary>
    /// Validates party and profile settings before the end user is allowed to instantiate a new app instance
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation.</param>
    /// <param name="partyId">The selected partyId</param>
    /// <returns>A validation status</returns>
    [Authorize]
    [HttpPost("{org}/{app}/api/v1/parties/validateInstantiation")]
    public async Task<IActionResult> ValidateInstantiation(string org, string app, [FromQuery] int partyId)
    {
        UserContext userContext = await _userHelper.GetUserContext(HttpContext);
        UserProfile? user = await _profileClient.GetUserProfile(userContext.UserId);
        if (user is null)
        {
            return StatusCode(500, "Could not get user profile while validating instantiation");
        }
        List<Party>? partyList = await _authorizationClient.GetPartyList(userContext.UserId);
        Application application = await _appMetadata.GetApplicationMetadata();

        PartyTypesAllowed partyTypesAllowed = application.PartyTypesAllowed;
        Party? partyUserRepresents = null;

        // Check if the user can represent the supplied partyId
        if (partyId != user.PartyId)
        {
            Party? represents = InstantiationHelper.GetPartyByPartyId(partyList, partyId);
            if (represents == null)
            {
                // the user does not represent the chosen party id, is not allowed to initiate
                return Ok(
                    new InstantiationValidationResult
                    {
                        Valid = false,
                        Message = "The user does not represent the supplied party",
                        ValidParties = InstantiationHelper.FilterPartiesByAllowedPartyTypes(
                            partyList,
                            partyTypesAllowed
                        ),
                    }
                );
            }

            partyUserRepresents = represents;
        }

        if (partyUserRepresents == null)
        {
            // if not set, the user represents itself
            partyUserRepresents = user.Party;
        }

        // Check if the application can be initiated with the party chosen
        bool canInstantiate = InstantiationHelper.IsPartyAllowedToInstantiate(partyUserRepresents, partyTypesAllowed);

        if (!canInstantiate)
        {
            return Ok(
                new InstantiationValidationResult
                {
                    Valid = false,
                    Message = "The supplied party is not allowed to instantiate the application",
                    ValidParties = InstantiationHelper.FilterPartiesByAllowedPartyTypes(partyList, partyTypesAllowed),
                }
            );
        }

        return Ok(new InstantiationValidationResult { Valid = true });
    }

    /// <summary>
    /// Updates the party the user represents
    /// </summary>
    /// <returns>Status code</returns>
    [Authorize]
    [HttpPut("{org}/{app}/api/v1/parties/{partyId}")]
    public async Task<IActionResult> UpdateSelectedParty(int partyId)
    {
        UserContext userContext = await _userHelper.GetUserContext(HttpContext);
        int userId = userContext.UserId;

        bool? isValid = await _authorizationClient.ValidateSelectedParty(userId, partyId);

        if (!isValid.HasValue)
        {
            return StatusCode(500, "Something went wrong when trying to update selectedparty.");
        }
        else if (isValid.Value == false)
        {
            return BadRequest($"User {userId} cannot represent party {partyId}.");
        }

        Response.Cookies.Append(
            _settings.GetAltinnPartyCookieName,
            partyId.ToString(CultureInfo.InvariantCulture),
            new CookieOptions { Domain = _settings.HostName }
        );

        return Ok("Party successfully updated");
    }
}
