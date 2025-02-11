using System.Text.Json;
using System.Web;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Provides access to the default home view.
/// </summary>
public class HomeController : Controller
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAntiforgery _antiforgery;
    private readonly PlatformSettings _platformSettings;
    private readonly IWebHostEnvironment _env;
    private readonly AppSettings _appSettings;
    private readonly IAppResources _appResources;
    private readonly IAppMetadata _appMetadata;
    private readonly List<string> _onEntryWithInstance = new List<string> { "new-instance", "select-instance" };

    private readonly ILogger<ApplicationMetadataController> _logger;
    private readonly FrontEndSettings _frontEndSettings;
    private readonly IProfileClient _profileClient;

    private readonly IAuthorizationClient _authorizationClient;
    private readonly UserHelper _userHelper;
    private readonly GeneralSettings _settings;

    /// <summary>
    /// Initialize a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    /// <param name="antiforgery">The anti forgery service.</param>
    /// <param name="platformSettings">The platform settings.</param>
    /// <param name="env">The current environment.</param>
    /// <param name="appSettings">The application settings</param>
    /// <param name="appResources">The application resources service</param>
    /// <param name="appMetadata">The application metadata service</param>
    public HomeController(
        IAntiforgery antiforgery,
        IOptions<PlatformSettings> platformSettings,
        IWebHostEnvironment env,
        IOptions<AppSettings> appSettings,
        IAppResources appResources,
        IAppMetadata appMetadata,
        ILogger<ApplicationMetadataController> logger,
        IOptions<FrontEndSettings> frontEndSettings,
        IProfileClient profileClient,
        IAuthorizationClient authorizationClient,
        IOptions<GeneralSettings> settings,
        IAltinnPartyClient altinnPartyClientClient
    )
    {
        _antiforgery = antiforgery;
        _platformSettings = platformSettings.Value;
        _env = env;
        _appSettings = appSettings.Value;
        _appResources = appResources;
        _appMetadata = appMetadata;
        _appMetadata = appMetadata;
        _logger = logger;
        _frontEndSettings = frontEndSettings.Value;
        _profileClient = profileClient;
        _authorizationClient = authorizationClient;
        _userHelper = new UserHelper(profileClient, altinnPartyClientClient, settings);
    }

    /// <summary>
    /// Returns the index view with references to the React app.
    /// </summary>
    /// <param name="org">The application owner short name.</param>
    /// <param name="app">The name of the app</param>
    /// <param name="dontChooseReportee">Parameter to indicate disabling of reportee selection in Altinn Portal.</param>
    [HttpGet]
    [Route("{org}/{app}/")]
    public async Task<IActionResult> Index(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromQuery] bool dontChooseReportee
    )
    {
        // See comments in the configuration of Antiforgery in MvcConfiguration.cs.
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        if (tokens.RequestToken != null)
        {
            HttpContext.Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false, // Make this cookie readable by Javascript.
                }
            );
        }

        if (await ShouldShowAppView())
        {
            ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();

            string wantedAppId = $"{org}/{app}";

            var initialState = new InitialState(application);

            //Adding key from _appSettings to be backwards compatible.
            if (
                !_frontEndSettings.ContainsKey(nameof(_appSettings.AppOidcProvider))
                && !string.IsNullOrEmpty(_appSettings.AppOidcProvider)
            )
            {
                _frontEndSettings.Add(nameof(_appSettings.AppOidcProvider), _appSettings.AppOidcProvider);
            }

            //return new JsonResult(frontEndSettings, _jsonSerializerOptions);
            initialState.FrontEndSettings = _frontEndSettings; // JsonSerializer.Serialize(initialState, _jsonSerializerOptions); //new JsonResult(_frontEndSettings, _jsonSerializerOptions);

            int userId = AuthenticationHelper.GetUserId(HttpContext);
            if (userId == 0)
            {
                return BadRequest("The userId is not provided in the context.");
            }

            try
            {
                var user = await _profileClient.GetUserProfile(userId);

                if (user == null)
                {
                    return NotFound();
                }

                initialState.User = user;
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

            UserContext userContext = await _userHelper.GetUserContext(HttpContext);
            List<Party>? partyList = await _authorizationClient.GetPartyList(userContext.UserId);

            List<Party> validParties = InstantiationHelper.FilterPartiesByAllowedPartyTypes(
                partyList,
                application.PartyTypesAllowed
            );

            initialState.ValidParties = validParties;

            ViewBag.InitialState = JsonSerializer.Serialize(initialState, _jsonSerializerOptions);

            ViewBag.org = org;
            ViewBag.app = app;
            return PartialView("Index");
        }

        string scheme = _env.IsDevelopment() ? "http" : "https";
        string goToUrl = HttpUtility.UrlEncode($"{scheme}://{Request.Host}/{org}/{app}");

        string redirectUrl = $"{_platformSettings.ApiAuthenticationEndpoint}authentication?goto={goToUrl}";

        if (!string.IsNullOrEmpty(_appSettings.AppOidcProvider))
        {
            redirectUrl += "&iss=" + _appSettings.AppOidcProvider;
        }

        if (dontChooseReportee)
        {
            redirectUrl += "&DontChooseReportee=true";
        }

        return Redirect(redirectUrl);
    }

    private async Task<bool> ShouldShowAppView()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return true;
        }

        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();
        if (!IsStatelessApp(application))
        {
            return false;
        }

        DataType? dataType = GetStatelessDataType(application);

        if (dataType != null && dataType.AppLogic.AllowAnonymousOnStateless)
        {
            return true;
        }

        return false;
    }

    private bool IsStatelessApp(ApplicationMetadata application)
    {
        if (application?.OnEntry == null)
        {
            return false;
        }

        return !_onEntryWithInstance.Contains(application.OnEntry.Show);
    }

    private DataType? GetStatelessDataType(ApplicationMetadata application)
    {
        string layoutSetsString = _appResources.GetLayoutSets();

        // Stateless apps only work with layousets
        if (!string.IsNullOrEmpty(layoutSetsString))
        {
            LayoutSets? layoutSets = JsonSerializer.Deserialize<LayoutSets>(layoutSetsString, _jsonSerializerOptions);
            string? dataTypeId = layoutSets?.Sets?.Find(set => set.Id == application.OnEntry?.Show)?.DataType;
            return application.DataTypes.Find(d => d.Id == dataTypeId);
        }

        return null;
    }
}
