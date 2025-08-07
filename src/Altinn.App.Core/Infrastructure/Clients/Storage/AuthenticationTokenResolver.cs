using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

/// <summary>
/// Resolves authentication tokens based on the specified authentication method.
/// </summary>
public interface IAuthenticationTokenResolver
{
    /// <summary>
    /// Retrieves an access token based on the specified authentication method.
    /// </summary>
    Task<JwtToken> GetAccessToken(
        AuthenticationMethod authenticationMethod,
        CancellationToken cancellationToken = default
    );
}

internal class AuthenticationTokenResolver : IAuthenticationTokenResolver
{
    private readonly IUserTokenProvider _userTokenProvider;
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly IAppMetadata _appMetadata;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<PlatformSettings> _platformSettings;

    private readonly bool _isDev;

    public AuthenticationTokenResolver(
        IHttpClientFactory httpClientFactory,
        IUserTokenProvider userTokenProvider,
        IMaskinportenClient maskinportenClient,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IOptionsMonitor<PlatformSettings> platformSettings
    )
    {
        _userTokenProvider = userTokenProvider;
        _maskinportenClient = maskinportenClient;
        _appMetadata = appMetadata;
        _httpClientFactory = httpClientFactory;
        _platformSettings = platformSettings;
        _isDev = hostEnvironment.IsDevelopment();
    }

    /// <inheritdoc />
    public async Task<JwtToken> GetAccessToken(
        AuthenticationMethod authenticationMethod,
        CancellationToken cancellationToken = default
    )
    {
        return authenticationMethod switch
        {
            AuthenticationMethod.UserToken => GetCurrentUserToken(),
            AuthenticationMethod.AltinnToken request when _isDev => await GetLocalTestToken(request, cancellationToken),
            AuthenticationMethod.AltinnToken request => await GetAltinnToken(request, cancellationToken),
            AuthenticationMethod.MaskinportenToken request => await GetMaskinportenToken(request, cancellationToken),
            AuthenticationMethod.CustomToken request => await request.TokenProvider.Invoke(),
            _ => throw new ArgumentException($"Invalid authentication method '{authenticationMethod.GetType().Name}'"),
        };
    }

    private async Task<JwtToken> GetMaskinportenToken(
        AuthenticationMethod.MaskinportenToken request,
        CancellationToken cancellationToken
    ) => await _maskinportenClient.GetAccessToken(request.Scopes, cancellationToken);

    private async Task<JwtToken> GetAltinnToken(
        AuthenticationMethod.AltinnToken request,
        CancellationToken cancellationToken
    ) => await _maskinportenClient.GetAltinnExchangedToken(request.Scopes, cancellationToken);

    private JwtToken GetCurrentUserToken()
    {
        var token = _userTokenProvider.GetUserToken();
        return JwtToken.Parse(token);
    }

    private async Task<JwtToken> GetLocalTestToken(
        AuthenticationMethod.AltinnToken request,
        CancellationToken cancellationToken
    )
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        string formattedScopes = MaskinportenClient.GetFormattedScopes(request.Scopes);
        string baseUrl = LocaltestValidation.GetLocaltestBaseUrl(_platformSettings.CurrentValue);
        string url =
            $"{baseUrl}/Home/GetTestOrgToken?org={appMetadata.Org}&orgNumber=991825827&authenticationLevel=3&scopes={Uri.EscapeDataString(formattedScopes)}";

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);

        await EnsureSuccessStatusCode(response);

        string token = await response.Content.ReadAsStringAsync(cancellationToken);
        return JwtToken.Parse(token);
    }

    private static async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw await PlatformHttpException.CreateAsync(response);
    }
}
