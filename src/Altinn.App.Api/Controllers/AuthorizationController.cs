using System.Globalization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Auth;
using Authorization.Platform.Authorization.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Exposes API endpoints related to authorization
/// </summary>
public class AuthorizationController : Controller
{
    private readonly IAuthorizationClient _authorization;
    private readonly GeneralSettings _settings;
    private readonly IAuthenticationContext _authenticationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationController"/> class
    /// </summary>
    public AuthorizationController(
        IAuthorizationClient authorization,
        IOptions<GeneralSettings> settings,
        IServiceProvider serviceProvider
    )
    {
        _authorization = authorization;
        _settings = settings.Value;
        _authenticationContext = serviceProvider.GetRequiredService<IAuthenticationContext>();
    }

    /// <summary>
    /// Gets current party by reading cookie value and validating.
    /// </summary>
    /// <returns>Party id for selected party. If invalid, partyId for logged in user is returned.</returns>
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("{org}/{app}/api/authorization/parties/current")]
    public async Task<ActionResult> GetCurrentParty(bool returnPartyObject = false)
    {
        var context = _authenticationContext.Current;
        switch (context)
        {
            case AuthenticationInfo.Unauthenticated:
                return Unauthorized();
            case AuthenticationInfo.User user:
            {
                var details = await user.LoadDetails(validateSelectedParty: true);
                if (details.CanRepresent is not bool canRepresent)
                    throw new Exception("Couldn't validate selected party");

                if (canRepresent)
                {
                    if (returnPartyObject)
                    {
                        return Ok(details.Reportee);
                    }

                    return Ok(details.Reportee.PartyId);
                }

                // Now we know the user can't represent the selected party (reportee)
                // so we will automatically switch to the user's own party (from the profile)
                var reportee = details.Profile.Party;
                if (user.SelectedPartyId is null || user.SelectedPartyId.Value != reportee.PartyId)
                {
                    // Setting cookie to partyID of logged in user if it varies from previus value.
                    Response.Cookies.Append(
                        _settings.GetAltinnPartyCookieName,
                        reportee.PartyId.ToString(CultureInfo.InvariantCulture),
                        new CookieOptions { Domain = _settings.HostName }
                    );
                }

                if (returnPartyObject)
                {
                    return Ok(reportee);
                }
                return Ok(reportee.PartyId);
            }
            case AuthenticationInfo.SelfIdentifiedUser selfIdentified:
            {
                var details = await selfIdentified.LoadDetails();
                if (returnPartyObject)
                {
                    return Ok(details.Reportee);
                }

                return Ok(details.Reportee.PartyId);
            }
            case AuthenticationInfo.Org org:
            {
                var details = await org.LoadDetails();
                if (returnPartyObject)
                {
                    return Ok(details.Party);
                }

                return Ok(details.Party.PartyId);
            }
            case AuthenticationInfo.ServiceOwner so:
            {
                var details = await so.LoadDetails();
                if (returnPartyObject)
                {
                    return Ok(details.Party);
                }

                return Ok(details.Party.PartyId);
            }
            case AuthenticationInfo.SystemUser su:
            {
                var details = await su.LoadDetails();
                if (returnPartyObject)
                {
                    return Ok(details.Party);
                }

                return Ok(details.Party.PartyId);
            }
            default:
                throw new Exception($"Unknown authentication context: {context.GetType().Name}");
        }
    }

    /// <summary>
    /// Checks if the user can represent the selected party.
    /// </summary>
    /// <param name="userId">The userId</param>
    /// <param name="partyId">The partyId</param>
    /// <returns>Boolean indicating if the selected party is valid.</returns>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ValidateSelectedParty(int userId, int partyId)
    {
        if (partyId == 0 || userId == 0)
        {
            return BadRequest("Both userId and partyId must be provided.");
        }

        bool? result = await _authorization.ValidateSelectedParty(userId, partyId);

        if (result != null)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, $"Something went wrong when trying to validate party {partyId} for user {userId}");
        }
    }

    /// <summary>
    /// Fetches roles for current party.
    /// </summary>
    /// <returns>List of roles for the current user and party.</returns>
    // [Authorize]
    // [HttpGet("{org}/{app}/api/authorization/roles")]
    // [ProducesResponseType(typeof(IEnumerable<Role), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Authorize]
    [HttpGet("{org}/{app}/api/authorization/roles")]
    [ProducesResponseType(typeof(IEnumerable<Role>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRolesForCurrentParty()
    {
        var context = _authenticationContext.Current;
        switch (context)
        {
            case AuthenticationInfo.Unauthenticated:
                return Unauthorized();
            case AuthenticationInfo.User user:
            {
                var details = await user.LoadDetails(validateSelectedParty: true);
                if (details.CanRepresent is not bool canRepresent)
                    throw new Exception("Couldn't validate selected party");
                if (!canRepresent)
                    return Unauthorized();

                return Ok(details.Roles);
            }
            case AuthenticationInfo.SelfIdentifiedUser:
            {
                return Ok(Array.Empty<Role>());
            }
            case AuthenticationInfo.Org:
            {
                return Ok(Array.Empty<Role>());
            }
            case AuthenticationInfo.ServiceOwner:
            {
                return Ok(Array.Empty<Role>());
            }
            case AuthenticationInfo.SystemUser su:
            {
                // TODO: is there an API for role lookup for system users?
                return Ok(Array.Empty<Role>());
            }
            default:
                throw new Exception($"Unknown authentication context: {context.GetType().Name}");
        }
    }
}
