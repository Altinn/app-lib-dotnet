using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Hanldes application metadata
/// AllowAnonymous, because this is static known information and used from LocalTest
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("{org}/{app}/api/v1/inital-state")]
public class InitialStateController : ControllerBase
{
    private readonly IAppMetadata _appMetadata;
    private readonly ILogger<ApplicationMetadataController> _logger;
    private readonly AppSettings _appSettings;
    private readonly FrontEndSettings _frontEndSettings;
    private readonly IProfileClient _profileClient;

    private readonly IAuthorizationClient _authorizationClient;
    private readonly UserHelper _userHelper;
    private readonly GeneralSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMetadataController"/> class
    /// <param name="appMetadata">The IAppMetadata service</param>
    /// <param name="logger">Logger for ApplicationMetadataController</param>
    /// </summary>
    public InitialStateController(
        IAppMetadata appMetadata,
        ILogger<ApplicationMetadataController> logger,
        IOptions<AppSettings> appSettings,
        IOptions<FrontEndSettings> frontEndSettings,
        IProfileClient profileClient,
        IAuthorizationClient authorizationClient,
        IAltinnPartyClient altinnPartyClientClient,
        IOptions<GeneralSettings> settings
    )
    {
        _appMetadata = appMetadata;
        _logger = logger;
        _appSettings = appSettings.Value;
        _frontEndSettings = frontEndSettings.Value;
        _profileClient = profileClient;
        _authorizationClient = authorizationClient;
        _userHelper = new UserHelper(profileClient, altinnPartyClientClient, settings);
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Get the application metadata https://altinncdn.no/schemas/json/application/application-metadata.schema.v1.json
    ///
    /// If org and app does not match, this returns a 409 Conflict response
    /// </summary>
    /// <param name="org">Unique identifier of the organisation responsible for the app.</param>
    /// <param name="app">Application identifier which is unique within an organisation.</param>
    /// <param name="checkOrgApp">Boolean get parameter to skip verification of correct org/app</param>
    /// <returns>Application metadata</returns>
    [HttpGet()]
    public async Task<ActionResult<InitialState>> GetAction(
        string org,
        string app,
        [FromQuery] bool checkOrgApp = true,
        bool allowedToInstantiateFilter = false
    )
    {
        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();

        string wantedAppId = $"{org}/{app}";

        var initialState = new InitialState(application);
        // {
        //     ApplicationMetadata = application,
        //     // Populate any additional properties of InitialState if necessary
        // };



        FrontEndSettings frontEndSettings = new FrontEndSettings();

        // Adding key from _appSettings to be backwards compatible.
        if (
            !frontEndSettings.ContainsKey(nameof(_appSettings.AppOidcProvider))
            && !string.IsNullOrEmpty(_appSettings.AppOidcProvider)
        )
        {
            frontEndSettings.Add(nameof(_appSettings.AppOidcProvider), _appSettings.AppOidcProvider);
        }

        //return new JsonResult(frontEndSettings, _jsonSerializerOptions);
        initialState.FrontEndSettings = new JsonResult(frontEndSettings, _jsonSerializerOptions);

        int userId = AuthenticationHelper.GetUserId(HttpContext);
        if (userId == 0)
        {
            return BadRequest("The userId is not proviced in the context.");
        }

        try
        {
            var user = await _profileClient.GetUserProfile(userId);

            if (user == null)
            {
                return NotFound();
            }

            initialState.User = user;

            //return Ok(user);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }

        UserContext userContext = await _userHelper.GetUserContext(HttpContext);
        List<Party>? partyList = await _authorizationClient.GetPartyList(userContext.UserId);

        if (allowedToInstantiateFilter)
        {
            List<Party> validParties = InstantiationHelper.FilterPartiesByAllowedPartyTypes(
                partyList,
                application.PartyTypesAllowed
            );
            //return Ok(validParties);
            initialState.ValidParties = validParties;
        }
        initialState.ValidParties = partyList;

        if (!checkOrgApp || application.Id.Equals(wantedAppId, StringComparison.Ordinal))
        {
            return Ok(initialState);
        }

        return Conflict($"This is {application.Id}, and not the app you are looking for: {wantedAppId}!");
    }
}
