using System.Text.Json;
using Altinn.App.Core.Features.Maskinporten.Exceptions;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Core.Features.Maskinporten;

/// <inheritdoc/>
public sealed class MaskinportenClient : IMaskinportenClient
{
    /// <summary>
    /// The margin to take into consideration when determining if a token has expired (seconds).
    /// <remarks>This value represents the worst-case latency scenario for <em>outbound</em> connections carrying the access token.</remarks>
    /// </summary>
    internal const int TokenExpirationMargin = 30;

    private readonly ILogger<MaskinportenClient> _logger;
    private readonly IOptionsMonitor<MaskinportenSettings> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeprovider;
    private readonly RefreshCache<string, MaskinportenTokenResponse> _tokenCache;
    private readonly Telemetry? _telemetry;

    /// <summary>
    /// Instantiates a new <see cref="MaskinportenClient"/> object.
    /// </summary>
    /// <param name="options">Maskinporten settings.</param>
    /// <param name="httpClientFactory">HttpClient factory.</param>
    /// <param name="logger">Logger interface.</param>
    /// <param name="timeProvider">Optional TimeProvider implementation.</param>
    /// <param name="telemetry">Optional telemetry service.</param>
    public MaskinportenClient(
        IOptionsMonitor<MaskinportenSettings> options,
        IHttpClientFactory httpClientFactory,
        ILogger<MaskinportenClient> logger,
        TimeProvider? timeProvider = null,
        Telemetry? telemetry = null
    )
    {
        _options = options;
        _telemetry = telemetry;
        _timeprovider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tokenCache = new RefreshCache<string, MaskinportenTokenResponse>(
            refetchBeforeExpiry: TimeSpan.FromSeconds(10),
            timeProvider: _timeprovider,
            maxCacheEntries: 256
        );
    }

    /// <inheritdoc/>
    public async Task<MaskinportenTokenResponse> GetAccessToken(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default
    )
    {
        string formattedScopes = FormattedScopes(scopes);
        DateTimeOffset referenceTime = _timeprovider.GetUtcNow();

        _telemetry?.StartGetAccessTokenActivity(_options.CurrentValue.ClientId, formattedScopes);

        return await _tokenCache.GetOrCreate(
            formattedScopes,
            valueFactory: async () =>
            {
                var token = await HandleMaskinportenAuthentication(formattedScopes, cancellationToken);
                var now = _timeprovider.GetUtcNow();

                var cacheExpiry = referenceTime.AddSeconds(token.ExpiresIn - TokenExpirationMargin);
                if (cacheExpiry <= now)
                {
                    throw new MaskinportenTokenExpiredException(
                        $"Access token cannot be used because it has a calculated expiration in the past (taking into account a margin of {TokenExpirationMargin} seconds): {token}"
                    );
                }

                return token;
            },
            lifetimeFactory: token =>
            {
                var now = _timeprovider.GetUtcNow();
                var timeSinceTokenCreation = now - referenceTime;
                var tokenLifespan = TimeSpan.FromSeconds(token.ExpiresIn - TokenExpirationMargin);

                return tokenLifespan - timeSinceTokenCreation;
            },
            postProcessCallback: (response, type) =>
            {
                var requestResult = type switch
                {
                    CacheResultType.Cached or CacheResultType.Refreshed => Telemetry.Maskinporten.RequestResult.Cached,
                    CacheResultType.Expired or CacheResultType.New => Telemetry.Maskinporten.RequestResult.New,
                    _ => Telemetry.Maskinporten.RequestResult.Error
                };
                _telemetry?.RecordMaskinportenTokenRequest(requestResult);
            }
        );
    }

    /// <summary>
    /// Handles the sending of grant requests to Maskinporten and parsing the returned response
    /// </summary>
    /// <param name="formattedScopes">A single space-separated string containing the scopes to authorize for.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns><inheritdoc cref="GetAccessToken"/></returns>
    /// <exception cref="MaskinportenAuthenticationException"><inheritdoc cref="GetAccessToken"/></exception>
    private async Task<MaskinportenTokenResponse> HandleMaskinportenAuthentication(
        string formattedScopes,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            string jwt = GenerateJwtGrant(formattedScopes);
            FormUrlEncodedContent payload = GenerateAuthenticationPayload(jwt);

            _logger.LogDebug(
                "Sending grant request to Maskinporten: {GrantRequest}",
                await payload.ReadAsStringAsync(cancellationToken)
            );

            string tokenAuthority = _options.CurrentValue.Authority.Trim('/');
            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await client.PostAsync(
                $"{tokenAuthority}/token",
                payload,
                cancellationToken
            );
            MaskinportenTokenResponse token = await ParseServerResponse(response, cancellationToken);

            _logger.LogDebug("Token retrieved successfully");
            return token;
        }
        catch (MaskinportenException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new MaskinportenAuthenticationException($"Authentication with Maskinporten failed: {e.Message}", e);
        }
    }

    /// <summary>
    /// Generates a JWT grant for the supplied scope claims along with the pre-configured client id and private key.
    /// </summary>
    /// <param name="formattedScopes">A space-separated list of scopes to make a claim for.</param>
    /// <returns><inheritdoc cref="JsonWebTokenHandler.CreateToken(SecurityTokenDescriptor)"/></returns>
    /// <exception cref="MaskinportenConfigurationException"></exception>
    internal string GenerateJwtGrant(string formattedScopes)
    {
        MaskinportenSettings? settings;
        try
        {
            settings = _options.CurrentValue;
        }
        catch (OptionsValidationException e)
        {
            throw new MaskinportenConfigurationException(
                $"Error reading MaskinportenSettings from the current app configuration",
                e
            );
        }

        var now = _timeprovider.GetUtcNow();
        var expiry = now.AddMinutes(2);
        var jwtDescriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.ClientId,
            Audience = settings.Authority,
            IssuedAt = now.UtcDateTime,
            Expires = expiry.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.Key, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object> { ["scope"] = formattedScopes, ["jti"] = Guid.NewGuid().ToString() }
        };

        return new JsonWebTokenHandler().CreateToken(jwtDescriptor);
    }

    /// <summary>
    /// <para>
    /// Generates an authentication payload from the supplied JWT (see <see cref="GenerateJwtGrant"/>).
    /// </para>
    /// <para>
    /// This payload needs to be a <see cref="FormUrlEncodedContent"/> object with some precise parameters,
    /// as per <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument#5-be-om-token">the docs.</a>.
    /// </para>
    /// </summary>
    /// <param name="jwtAssertion">The JWT token generated by <see cref="GenerateJwtGrant"/>.</param>
    internal static FormUrlEncodedContent GenerateAuthenticationPayload(string jwtAssertion)
    {
        return new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwtAssertion
            }
        );
    }

    /// <summary>
    /// Parses the Maskinporten server response and deserializes the JSON body.
    /// </summary>
    /// <param name="httpResponse">The server response.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A <see cref="MaskinportenTokenResponse"/> for successful requests.</returns>
    /// <exception cref="MaskinportenAuthenticationException">Authentication failed.
    /// This could be caused by an authentication/authorization issue or a myriad of tother circumstances.</exception>
    internal static async Task<MaskinportenTokenResponse> ParseServerResponse(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            string content = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new MaskinportenAuthenticationException(
                        $"Maskinporten authentication failed with status code {(int)httpResponse.StatusCode} ({httpResponse.StatusCode}): {content}"
                    );
                }

                return JsonSerializer.Deserialize<MaskinportenTokenResponse>(content)
                    ?? throw new JsonException("JSON body is null");
            }
            catch (JsonException e)
            {
                throw new MaskinportenAuthenticationException(
                    $"Maskinporten replied with invalid JSON formatting: {content}",
                    e
                );
            }
        }
        catch (MaskinportenException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new MaskinportenAuthenticationException($"Authentication with Maskinporten failed: {e.Message}", e);
        }
    }

    /// <summary>
    /// Formats a list of scopes according to the expected formatting (space-delimited).
    /// See <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument#5-be-om-token">the docs</a> for more information.
    /// </summary>
    /// <param name="scopes">A collection of scopes.</param>
    /// <returns>A single string containing the supplied scopes.</returns>
    internal static string FormattedScopes(IEnumerable<string> scopes) => string.Join(" ", scopes);
}
