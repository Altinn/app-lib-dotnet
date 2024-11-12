using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Maskinporten.Constants;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CorrespondenceResult = Altinn.App.Core.Features.Telemetry.Correspondence.CorrespondenceResult;

namespace Altinn.App.Core.Features.Correspondence;

/// <inheritdoc />
internal sealed class CorrespondenceClient : ICorrespondenceClient
{
    private readonly ILogger<CorrespondenceClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlatformSettings _platformSettings;
    private readonly Telemetry? _telemetry;

    public CorrespondenceClient(
        IHttpClientFactory httpClientFactory,
        IOptions<PlatformSettings> platformSettings,
        ILogger<CorrespondenceClient> logger,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _platformSettings = platformSettings.Value;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public async Task<CorrespondenceResponse> Send(
        SendCorrespondencePayload payload,
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
            _logger.LogDebug("Fetching access token via factory");
            string uri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/') + "/correspondence/upload";
            AccessToken accessToken = await payload.AccessTokenFactory();

            using MultipartFormDataContent content = payload.CorrespondenceRequest.Serialise();
            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpRequestMessage request = AuthenticatedHttpRequestFactory(
                method: HttpMethod.Post,
                uri: uri,
                content: content,
                accessToken: accessToken
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
                JsonSerializer.Deserialize<CorrespondenceResponse>(responseBody)
                ?? throw new CorrespondenceRequestException("Invalid response from Correspondence API server");

            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Success);
            return parsedResponse;
        }
        catch (CorrespondenceException e)
        {
            _logger.LogError(
                e,
                "Failed to send Correspondence: status={StatusCode} response={Response}",
                response?.StatusCode.ToString() ?? "Unknown",
                responseBody ?? "No response body"
            );
            activity?.Errored(e, problemDetails?.Detail);
            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Error);

            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Failed to send Correspondence: status={StatusCode} response={Response}",
                response?.StatusCode.ToString() ?? "Unknown",
                responseBody ?? "No response body"
            );
            activity?.Errored(e);
            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Error);

            throw new CorrespondenceRequestException($"Failed to send correspondence: {e}", e);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private HttpRequestMessage AuthenticatedHttpRequestFactory<TContent>(
        HttpMethod method,
        string uri,
        TContent content,
        AccessToken accessToken
    )
        where TContent : HttpContent
    {
        _logger.LogDebug("Constructing authorized http request for target uri {TargetEndpoint}", uri);
        HttpRequestMessage request = new(method, uri) { Content = content };

        request.Headers.Authorization = new AuthenticationHeaderValue(TokenTypes.Bearer, accessToken);
        request.Headers.TryAddWithoutValidation(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);

        return request;
    }

    private ProblemDetails? GetProblemDetails(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

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
