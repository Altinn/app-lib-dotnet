using System.Net.Http.Headers;
using Altinn.App.Core.Features.Maskinporten.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Maskinporten.Delegates;

/// <summary>
/// A <see cref="DelegatingHandler"/> middleware that provides authorization for all http requests
/// </summary>
internal sealed class MaskinportenDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<MaskinportenDelegatingHandler> _logger;
    private readonly IEnumerable<string> _scopes;
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
        _logger = logger;
        _scopes = scopes;
        _maskinportenClient = maskinportenClient;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Executing custom `SendAsync` method; injecting authentication headers");
        var auth = await _maskinportenClient.GetAccessToken(_scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
