using Altinn.App.Core.Internal.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller that exposes profile
/// </summary>
[Authorize]
[Route("{org}/{app}/api/v1/profile")]
[ApiController]
public class ProfileController : Controller
{
    private readonly IAuthenticationContext _authenticationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileController"/> class
    /// </summary>
    public ProfileController(IServiceProvider serviceProvider)
    {
        _authenticationContext = serviceProvider.GetRequiredService<IAuthenticationContext>();
    }

    /// <summary>
    /// Method that returns the user information about the user that is logged in
    /// </summary>
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("user")]
    public async Task<ActionResult> GetUser()
    {
        var context = _authenticationContext.Current;
        switch (context)
        {
            case AuthenticationInfo.User user:
            {
                var details = await user.LoadDetails(validateSelectedParty: false);
                return Ok(details.Profile);
            }
            default:
                return BadRequest("The userId is not proviced in the context.");
        }
    }
}
