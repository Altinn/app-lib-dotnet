using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IHttpClientBuilder"/>
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Ensure that the HttpClient will add the Authorization header from the incoming request to the outgoing request.
    /// </summary>
    /// <example>
    /// services.AddHttpClient&lt;IClient,Client&gt;()
    ///    .AddAuthToken()
    ///    .AddPlatformToken();
    /// </example>
    public static IHttpClientBuilder AddAuthToken(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddTransient<AuthTokenMessageHandler>();
        return builder.AddHttpMessageHandler<AuthTokenMessageHandler>();
    }

    /// <summary>
    /// Add platform token to the outgoing request.
    /// </summary>
    /// <example>
    /// services.AddHttpClient&lt;IClient,Client&gt;()
    ///    .AddAuthToken()
    ///    .AddPlatformToken();
    /// </example>
    public static IHttpClientBuilder AddPlatformToken(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.TryAddTransient<PlatformTokenMessageHandler>();
        return builder.AddHttpMessageHandler<PlatformTokenMessageHandler>();
    }

    private class PlatformTokenMessageHandler : DelegatingHandler
    {
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IAppMetadata _appMetadata;

        public PlatformTokenMessageHandler(IAccessTokenGenerator accessTokenGenerator, IAppMetadata appMetadata)
        {
            _accessTokenGenerator = accessTokenGenerator;
            _appMetadata = appMetadata;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var application = await _appMetadata.GetApplicationMetadata();
            var token = _accessTokenGenerator.GenerateAccessToken(application.Org, application.AppIdentifier.App);
            request.Headers.Add("PlatformAccessToken", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private class AuthTokenMessageHandler : DelegatingHandler
    {
        private readonly AppSettings _appSettings;
        private readonly HttpContext _httpContext;

        public AuthTokenMessageHandler(IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor.HttpContext);
            _httpContext = httpContextAccessor.HttpContext;
            _appSettings = appSettings.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the Authorization header from the incoming request
            var token = JwtTokenUtil.GetTokenFromContext(_httpContext, _appSettings.RuntimeCookieName);
            if (token is null)
            {
                throw new Exception($"No user token found in the incoming request suitable to forward to {request.RequestUri}");
            }

            // Add the Authorization header to the outgoing request
            request.Headers.Add("Authorization", $"Bearer {token}");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}