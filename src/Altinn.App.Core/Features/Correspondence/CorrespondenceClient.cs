using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Correspondence;

/// <inheritdoc />
internal sealed class CorrespondenceClient : ICorrespondenceClient
{
    private readonly ILogger<CorrespondenceClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly PlatformSettings _platformSettings;
    private readonly Telemetry _telemetry;

    public CorrespondenceClient(
        ILogger<CorrespondenceClient> logger,
        IHttpClientFactory httpClientFactory,
        [FromKeyedServices(MaskinportenClient.VariantInternal)] IMaskinportenClient maskinportenClient,
        IOptions<PlatformSettings> platformSettings,
        Telemetry telemetry
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _maskinportenClient = maskinportenClient;
        _platformSettings = platformSettings.Value;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task Send(Models.Correspondence correspondence, CancellationToken cancellationToken = default)
    {
        using Activity? activity = _telemetry.StartSendCorrespondenceActivity();
        HttpResponseMessage? response = null;
        string? responseBody = null;
        ProblemDetails? problemDetails = null;
        try
        {
            using MultipartFormDataContent content = new();
            correspondence.Serialize(content);

            // foreach (var attachment in correspondence.Content.Attachments ?? [])
            // {
            //     content.Add(new StreamContent(attachment.Data), "attachments", attachment.Filename ?? "");
            // }

            string uri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/') + "/correspondence/upload";
            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpRequestMessage request = await AuthenticatedHttpRequestFactory(
                method: HttpMethod.Post,
                uri: uri,
                content: content,
                scopes: ["altinn:correspondence.write"],
                cancellationToken
            );
            response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody);

                // TODO: handle errors here
            }

            // TODO: handle success here
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send Correspondence message: status={StatusCode} problem.type={ProblemType}",
                response?.StatusCode,
                problemDetails?.Type
            );
            activity?.Errored(ex);

            // TODO: handle errors here
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<HttpRequestMessage> AuthenticatedHttpRequestFactory<TContent>(
        HttpMethod method,
        string uri,
        TContent content,
        IEnumerable<string> scopes,
        CancellationToken cancellationToken
    )
        where TContent : HttpContent
    {
        HttpRequestMessage request = new(method, uri) { Content = content };

        var maskinportenToken = await _maskinportenClient.GetAccessToken(scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(TokenTypes.Bearer, maskinportenToken.AccessToken);
        request.Headers.TryAddWithoutValidation(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);

        return request;
    }
}
