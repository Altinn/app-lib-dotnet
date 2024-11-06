using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
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
    private readonly Telemetry? _telemetry;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public CorrespondenceClient(
        IHttpClientFactory httpClientFactory,
        [FromKeyedServices(MaskinportenClient.VariantInternal)] IMaskinportenClient maskinportenClient,
        IOptions<PlatformSettings> platformSettings,
        ILogger<CorrespondenceClient> logger,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _maskinportenClient = maskinportenClient;
        _platformSettings = platformSettings.Value;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<CorrespondenceResponse> Send(
        CorrespondenceRequest correspondenceRequest,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Sending Correspondence request");
        using Activity? activity = _telemetry?.StartSendCorrespondenceActivity();
        ProblemDetails? problemDetails = null;
        HttpResponseMessage? response = null;
        string? responseBody = null;
        try
        {
            string uri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/') + "/correspondence/upload";
            _logger.LogDebug("Correspondence uri is {CorrespondenceUri}", uri);

            using MultipartFormDataContent content = correspondenceRequest.Serialize();
            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpRequestMessage request = await AuthenticatedHttpRequestFactory(
                method: HttpMethod.Post,
                uri: uri,
                content: content,
                scopes: ["altinn:correspondence.write", "altinn:serviceowner/instances.read",],
                cancellationToken
            );
            response = await client.SendAsync(request, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                problemDetails = GetProblemDetails(responseBody);
                throw new CorrespondenceRequestException(
                    $"Correspondence request failed with status {response?.StatusCode}: {problemDetails?.Detail}"
                );
            }

            _logger.LogDebug("Correspondence request succeeded: {Response}", responseBody);
            var parsedResponse =
                JsonSerializer.Deserialize<CorrespondenceResponse>(responseBody, _jsonSerializerOptions)
                ?? throw new CorrespondenceRequestException("Invalid response from Correspondence API server");

            return parsedResponse;
        }
        catch (CorrespondenceException e)
        {
            _logger.LogError(
                e,
                "Failed to send Correspondence: status={StatusCode} response={Response}",
                response?.StatusCode,
                responseBody
            );
            activity?.Errored(e, problemDetails?.Detail);

            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to send Correspondence: status={StatusCode} response={Response}",
                response?.StatusCode.ToString() ?? "Unknown status code",
                responseBody ?? "No response body"
            );
            activity?.Errored(e);

            throw new CorrespondenceRequestException($"Failed to send correspondence: {e}", e);
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<CorrespondenceResponse> Send(
        ICorrespondenceBuilderCanBuild builder,
        CancellationToken cancellationToken = default
    )
    {
        return await Send(builder.Build(), cancellationToken);
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

        var altinnToken = await _maskinportenClient.GetAltinnExchangedToken(scopes, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(TokenTypes.Bearer, altinnToken.AccessToken);
        request.Headers.TryAddWithoutValidation(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);

        return request;
    }

    private ProblemDetails? GetProblemDetails(string responseBody)
    {
        try
        {
            return JsonSerializer.Deserialize<ProblemDetails>(responseBody);
        }
        catch (Exception e)
        {
            _logger.LogError("Error parsing ProblemDetails from correspondence api: {Error}", e);
        }

        return null;
    }
}
