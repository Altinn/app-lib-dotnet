using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Maskinporten.Delegates;

/// <summary>
/// A <see cref="DelegatingHandler"/> middleware that provides authorization for all http requests
/// </summary>
internal sealed class MaskinportenDelegatingHandler : DelegatingHandler
{
    public IEnumerable<string> Scopes { get; init; }

    private readonly ILogger<MaskinportenDelegatingHandler> _logger;
    private readonly IMaskinportenClient _maskinportenClient;

    /// <summary>
    /// Creates a new instance of <see cref="MaskinportenDelegatingHandler"/>.
    /// </summary>
    /// <param name="scopes">A list of scopes to claim authorization for with Maskinporten</param>
    /// <param name="maskinportenClient">A <see cref="MaskinportenClient"/> instance</param>
    /// <param name="logger">Optional logger interface</param>
    public MaskinportenDelegatingHandler(
        IEnumerable<string> scopes,
        IMaskinportenClient maskinportenClient,
        ILogger<MaskinportenDelegatingHandler> logger
    )
    {
        Scopes = scopes;
        _logger = logger;
        _maskinportenClient = maskinportenClient;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Executing custom `SendAsync` method; injecting authentication headers");
        var auth = await _maskinportenClient.GetAccessToken(Scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
