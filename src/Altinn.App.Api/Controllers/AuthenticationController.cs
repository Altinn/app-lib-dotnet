using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Internal.Auth;
using Altinn.Platform.Profile.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Exposes API endpoints related to authentication.
/// </summary>
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationClient _authenticationClient;
    private readonly GeneralSettings _settings;
    private readonly IAuthenticationContext _authenticationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationController"/> class
    /// </summary>
    public AuthenticationController(
        IAuthenticationClient authenticationClient,
        IOptions<GeneralSettings> settings,
        IAuthenticationContext authenticationContext
    )
    {
        _authenticationClient = authenticationClient;
        _settings = settings.Value;
        _authenticationContext = authenticationContext;
    }

    // /// <summary>
    // /// Gets current party by reading cookie value and validating.
    // /// </summary>
    // /// <returns>Party id for selected party. If invalid, partyId for logged in user is returned.</returns>
    // [Authorize]
    // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // [HttpGet("{org}/{app}/api/[controller]/current")]
    // public async Task<ActionResult> GetCurrent()
    // {
    //     bool returnPartyObject = false;
    // }

    // private sealed record CurrentAuthenticationResponse
    // {
    //     public required UserProfile? Profile { get; init; }
    // }

    /// <summary>
    /// Refreshes the AltinnStudioRuntime JwtToken when not in AltinnStudio mode.
    /// </summary>
    /// <returns>Ok result with updated token.</returns>
    [Authorize]
    [HttpGet("{org}/{app}/api/[controller]/keepAlive")]
    public async Task<IActionResult> KeepAlive()
    {
        string token = await _authenticationClient.RefreshToken();

        CookieOptions runtimeCookieSetting = new CookieOptions
        {
            Domain = _settings.HostName,
            HttpOnly = true,
            Secure = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            HttpContext.Response.Cookies.Append(General.RuntimeCookieName, token, runtimeCookieSetting);
            return Ok();
        }

        return BadRequest();
    }

    /// <summary>
    /// Invalidates the AltinnStudioRuntime cookie.
    /// </summary>
    /// <returns>Ok result with invalidated cookie.</returns>
    [Authorize]
    [HttpPut("{org}/{app}/api/[controller]/invalidatecookie")]
    public IActionResult InvalidateCookie()
    {
        HttpContext.Response.Cookies.Delete(
            General.RuntimeCookieName,
            new CookieOptions { Domain = _settings.HostName }
        );
        return Ok();
    }
}
