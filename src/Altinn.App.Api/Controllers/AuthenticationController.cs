using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Internal.Auth;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Authorization.Platform.Authorization.Models;
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
        IServiceProvider serviceProvider
    )
    {
        _authenticationClient = authenticationClient;
        _settings = settings.Value;
        _authenticationContext = serviceProvider.GetRequiredService<IAuthenticationContext>();
    }

    /// <summary>
    /// Gets current party by reading cookie value and validating.
    /// </summary>
    /// <returns>Party id for selected party. If invalid, partyId for logged in user is returned.</returns>
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("{org}/{app}/api/[controller]/current")]
    public async Task<ActionResult> GetCurrent()
    {
        var current = _authenticationContext.Current;

        CurrentAuthenticationBaseResponse response = current switch
        {
            AuthenticationInfo.Unauthenticated => new UnauthenticatedResponse(),
            AuthenticationInfo.User user when await user.LoadDetails(validateSelectedParty: true) is var details =>
                new UserResponse
                {
                    Profile = details.Profile,
                    Party = details.Reportee,
                    Parties = details.Parties,
                    PartiesAllowedToInstantiate = details.PartiesAllowedToInstantiate,
                    Roles = details.Roles,
                },
            AuthenticationInfo.Org org when await org.LoadDetails() is var details => new OrgResponse
            {
                Party = details.Party,
            },
            AuthenticationInfo.ServiceOwner serviceOwner when await serviceOwner.LoadDetails() is var details =>
                new ServiceOwnerResponse { Party = details.Party },
            AuthenticationInfo.SystemUser => new SystemUserResponse(),
            _ => throw new Exception("Unhandled authenticated type: " + current.GetType().Name),
        };

        return Ok(response);
    }

    [JsonDerivedType(typeof(UnauthenticatedResponse), typeDiscriminator: "Unauthenticated")]
    [JsonDerivedType(typeof(UserResponse), typeDiscriminator: "User")]
    [JsonDerivedType(typeof(OrgResponse), typeDiscriminator: "Org")]
    [JsonDerivedType(typeof(ServiceOwnerResponse), typeDiscriminator: "ServiceOwner")]
    [JsonDerivedType(typeof(SystemUserResponse), typeDiscriminator: "SystemUser")]
    private abstract record CurrentAuthenticationBaseResponse { }

    private sealed record UnauthenticatedResponse : CurrentAuthenticationBaseResponse { }

    private sealed record UserResponse : CurrentAuthenticationBaseResponse
    {
        public required UserProfile Profile { get; init; }

        public required Party Party { get; init; }

        public required IReadOnlyList<Party> Parties { get; init; }

        public required IReadOnlyList<Party> PartiesAllowedToInstantiate { get; init; }

        public required IReadOnlyList<Role> Roles { get; init; }
    }

    private sealed record OrgResponse : CurrentAuthenticationBaseResponse
    {
        public required Party Party { get; init; }
    }

    private sealed record ServiceOwnerResponse : CurrentAuthenticationBaseResponse
    {
        public required Party Party { get; init; }
    }

    private sealed record SystemUserResponse : CurrentAuthenticationBaseResponse { }

    // private sealed record PartyResponse(
    //     int PartyId,
    //     string? PartyUuid,
    //     PartyType PartyTypeName,
    //     string? OrgNumber,
    //     string? Ssn,
    //     string? UnitType,
    //     string Name,
    //     bool IsDeleted,
    //     bool OnlyHierarchyElementWithNoAccess
    // );

    // private enum PartyType : byte
    // {
    //     Person = 1,
    //     Organisation = 2,
    //     SelfIdentified = 3,
    //     SubUnit = 4,
    //     BankruptcyEstate = 5,
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
