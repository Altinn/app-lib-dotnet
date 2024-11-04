using System.Net.Http.Headers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Authentication;

/// <summary>
/// A client for authentication actions in Altinn Platform.
/// </summary>
public class AuthenticationClient : IAuthenticationClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly IClientContext _clientContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationClient"/> class
    /// </summary>
    /// <param name="platformSettings">The current platform settings.</param>
    /// <param name="logger">the logger</param>
    /// <param name="httpClient">A HttpClient provided by the HttpClientFactory.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public AuthenticationClient(
        IOptions<PlatformSettings> platformSettings,
        ILogger<AuthenticationClient> logger,
        HttpClient httpClient,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        httpClient.BaseAddress = new Uri(platformSettings.Value.ApiAuthenticationEndpoint);
        httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client = httpClient;
        _clientContext = serviceProvider.GetRequiredService<IClientContext>();
    }

    /// <inheritdoc />
    public async Task<string> RefreshToken()
    {
        string endpointUrl = $"refresh";
        string token = _clientContext.Current.Token; // TODO: check if authenticated?
        HttpResponseMessage response = await _client.GetAsync(token, endpointUrl);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            string refreshedToken = await response.Content.ReadAsStringAsync();
            refreshedToken = refreshedToken.Replace('"', ' ').Trim();
            return refreshedToken;
        }

        _logger.LogError($"Refreshing JwtToken failed with status code {response.StatusCode}");
        return string.Empty;
    }
}
