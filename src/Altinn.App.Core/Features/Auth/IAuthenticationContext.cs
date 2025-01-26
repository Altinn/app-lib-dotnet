using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Auth;

internal static class AuthenticationContextDI
{
    internal static void AddAuthenticationContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuthenticationContext, AuthenticationContext>();
    }
}

/// <summary>
/// Provides access to the current authentication context.
/// </summary>
public interface IAuthenticationContext
{
    /// <summary>
    /// The current authentication info.
    /// </summary>
    Authenticated Current { get; }
}

internal sealed class AuthenticationContext : IAuthenticationContext
{
    private const string ItemsKey = "Internal_AltinnAuthenticationInfo";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<AppSettings> _appSettings;
    private readonly IOptionsMonitor<GeneralSettings> _generalSettings;
    private readonly IProfileClient _profileClient;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly IAuthorizationClient _authorizationClient;
    private readonly IAppMetadata _appMetadata;

    public AuthenticationContext(
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<AppSettings> appSettings,
        IOptionsMonitor<GeneralSettings> generalSettings,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClient,
        IAuthorizationClient authorizationClient,
        IAppMetadata appMetadata
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _appSettings = appSettings;
        _generalSettings = generalSettings;
        _profileClient = profileClient;
        _altinnPartyClient = altinnPartyClient;
        _authorizationClient = authorizationClient;
        _appMetadata = appMetadata;
    }

    // Currently we're coupling this to the HTTP context directly.
    // In the future we might want to run work (e.g. service tasks) in the background,
    // at which point we won't always have a HTTP context available.
    // At that point we probably want to implement something like an `IExecutionContext`, `IExecutionContextAccessor`
    // to decouple ourselves from the ASP.NET request context.
    // TODO: consider removing dependcy on HTTP context
    private HttpContext _httpContext =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context available");

    public Authenticated Current
    {
        get
        {
            var httpContext = _httpContext;

            Authenticated authInfo;
            if (!httpContext.Items.TryGetValue(ItemsKey, out var authInfoObj))
            {
                authInfo = Authenticated.From(
                    tokenStr: JwtTokenUtil.GetTokenFromContext(
                        httpContext,
                        _appSettings.CurrentValue.RuntimeCookieName
                    ),
                    isAuthenticated: httpContext.User?.Identity?.IsAuthenticated ?? false,
                    () => _httpContext.Request.Cookies[_generalSettings.CurrentValue.GetAltinnPartyCookieName],
                    _profileClient.GetUserProfile,
                    _altinnPartyClient.GetParty,
                    (string orgNr) => _altinnPartyClient.LookupParty(new PartyLookup { OrgNo = orgNr }),
                    _authorizationClient.GetPartyList,
                    _authorizationClient.ValidateSelectedParty,
                    _authorizationClient.GetUserRoles,
                    _appMetadata.GetApplicationMetadata
                );
                httpContext.Items[ItemsKey] = authInfo;
            }
            else
            {
                authInfo =
                    authInfoObj as Authenticated
                    ?? throw new Exception("Unexpected type for authentication info in HTTP context");
            }
            return authInfo;
        }
    }
}
