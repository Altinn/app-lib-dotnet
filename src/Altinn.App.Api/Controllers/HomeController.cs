using System.Diagnostics;
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
[ApiController]
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

    //private readonly ApplicationMetadata _applicationMetadata;

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
        //   _applicationMetadata = applicationMetadata;
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
        // Access all query parameters
        var allQueryParams = HttpContext.Request.Query;

        foreach (var param in allQueryParams)
        {
            // Log each query parameter key and value
            Console.WriteLine($"{param.Key}: {param.Value}");
            HttpContext.Session.SetString(param.Key, param.Value);
            var value = HttpContext.Session.GetString(param.Key);
            Debugger.Break(); // This acts like a breakpoint.
        }

        //Debugger.Break(); // This acts like a breakpoint.

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

        Debugger.Break();

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
    /// <returns></returns>
    [HttpGet]
    [Route("{org}/{app}/set-query-params")]
    public async Task<IActionResult> SetQueryParams(string org, string app)
    {
        var queryParams = HttpContext.Request.Query;

        Application application = await _appMetadata.GetApplicationMetadata();

        List<string> dataTypes = application.DataTypes.Select(type => type.Id).ToList();

        List<string> allowedQueryParams = GetAllowedQueryParams(dataTypes);

        if (allowedQueryParams.Count < 1)
        {
            return Content("<h1>No query parameters found in the request.</h1>", "text/html");
        }

        var queryDict = allowedQueryParams.ToDictionary(q => q.Key, q => q.Value.ToString());
        var queryParamsJson = System.Text.Json.JsonSerializer.Serialize(queryDict);
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
            <script>
                const params = {queryParamsJson};
                sessionStorage.setItem('queryParams', JSON.stringify(params));
                const redirectUrl = `${{window.location.origin}}/{org}/{app}`;
                window.location.href = redirectUrl;
            </script>
        </body>
        </html>";

        return Content(htmlContent, "text/html");
    }

    private List<string> GetAllowedQueryParams(List<string> dataTypes)
    {
        return dataTypes
            .Select(item =>
            {
                var prefillJson = _appResources.GetPrefillJson(item);
                if (prefillJson == null)
                {
                    return null;
                }

                JObject prefillConfiguration = JObject.Parse(prefillJson);
                JToken? queryParamObject = prefillConfiguration.SelectToken("QueryParams");

                if (queryParamObject != null && queryParamObject.Type == JTokenType.Object)
                {
                    return ((JObject)queryParamObject).Properties().Select(prop => prop.Name).ToList();
                }

                return null;
            })
            .Where(result => result != null)
            .SelectMany(result => result)
            .Distinct()
            .ToList();
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
