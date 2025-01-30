using System.Text.Json;
using System.Web;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

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
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

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
        IAppMetadata appMetadata
    )
    {
        _antiforgery = antiforgery;
        _platformSettings = platformSettings.Value;
        _env = env;
        _appSettings = appSettings.Value;
        _appResources = appResources;
        _appMetadata = appMetadata;
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

    /// <summary>
    /// Sets query parameters in frontend session storage
    /// </summary>
    /// <param name="org"></param>
    /// <param name="app"></param>
    /// <returns>An HTML file with a small javascript that will set session variables in frontend and redirect to the app.</returns>
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("{org}/{app}/set-query-params")]
    public async Task<IActionResult> SetQueryParams(string org, string app)
    {
        var queryParams = HttpContext.Request.Query;

        Application application = await _appMetadata.GetApplicationMetadata();

        List<string> dataTypes = application.DataTypes.Select(type => type.Id).ToList();

        // Build the modelPrefill dictionary
        var modelPrefill = dataTypes
            .Select(item =>
            {
                var prefillJson = _appResources.GetPrefillJson(item);
                if (string.IsNullOrEmpty(prefillJson))
                {
                    return null;
                }

                return new { DataModelName = item, PrefillConfiguration = JObject.Parse(prefillJson) };
            })
            .Where(item => item != null)
            .ToList();

        var result = modelPrefill
            .Select(entry =>
            {
                if (entry == null || entry.PrefillConfiguration == null)
                {
                    return null;
                }

                var prefillConfig = entry.PrefillConfiguration;

                if (prefillConfig == null)
                {
                    return null;
                }

                var queryParamsConfig = prefillConfig["QueryParams"];
                if (queryParamsConfig == null || queryParamsConfig.Type != JTokenType.Object)
                {
                    return null;
                }

                // Filter allowed query parameters. We only allow query params that are configured in
                // <datamodel_name>.prefill.json
                var allowedQueryParams = ((JObject)queryParamsConfig)
                    .Properties()
                    .Where(prop => queryParams.ContainsKey(prop.Name))
                    .Select(prop => new Dictionary<string, string>
                    {
                        { prop.Value.ToString(), queryParams[prop.Name].ToString() },
                    })
                    .ToList();

                return new
                {
                    dataModelName = entry.DataModelName,
                    appId = application.Id,
                    prefillFields = allowedQueryParams,
                };
            })
            .Where(entry => entry != null && entry.prefillFields != null && entry.prefillFields.Count > 0)
            .ToList();

        var safeResultJson = System.Text.Json.JsonSerializer.Serialize(result, _jsonOptions);
        var encodedAppId = Uri.EscapeDataString(application.Id);

        string nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));

        var htmlContent =
            $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Set Query Params</title>
        </head>
        <body>
            <script nonce='{nonce}'>
                const prefillData = {safeResultJson};
                sessionStorage.setItem('queryParams', JSON.stringify(prefillData));
                const appOrg = decodeURIComponent('{encodedAppId}');
                window.location.href = `${{window.location.origin}}/${{appOrg}}`;
            </script>
        </body>
        </html>";

        Response.Headers["Content-Security-Policy"] = $"default-src 'self'; script-src 'nonce-{nonce}';";

        return Content(htmlContent, "text/html");
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
