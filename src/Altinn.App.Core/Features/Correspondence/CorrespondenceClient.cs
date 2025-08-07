using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Correspondence.Models.Response;
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

    private readonly CorrespondenceAuthorisationFactory _authorisationFactory;

    public CorrespondenceClient(
        IHttpClientFactory httpClientFactory,
        IOptions<PlatformSettings> platformSettings,
        IServiceProvider serviceProvider,
        ILogger<CorrespondenceClient> logger,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _platformSettings = platformSettings.Value;
        _telemetry = telemetry;
        _authorisationFactory = new CorrespondenceAuthorisationFactory(serviceProvider);
    }

    /// <inheritdoc />
    public async Task<SendCorrespondenceResponse> Send(
        SendCorrespondencePayload payload,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Sending Correspondence request");
        using Activity? activity = _telemetry?.StartSendCorrespondenceActivity();

        try
        {
            if (
                payload.CorrespondenceRequest.Content.Attachments?.Count > 0
                && payload.CorrespondenceRequest.Content.Attachments.All(a => a is CorrespondenceStreamedAttachment)
            )
            {
                var pollingJobs = new List<Task>();
                var premadeAttachments = new List<Guid>();
                foreach (
                    CorrespondenceStreamedAttachment attachment in payload.CorrespondenceRequest.Content.Attachments
                )
                {
                    var initializeAttachmentPayload = new AttachmentPayload
                    {
                        DisplayName = attachment.Filename,
                        FileName = attachment.Filename,
                        IsEncrypted = attachment.IsEncrypted ?? false,
                        ResourceId = payload.CorrespondenceRequest.ResourceId,
                        SendersReference = payload.CorrespondenceRequest.SendersReference + " - " + attachment.Filename,
                    };
                    var initializeAttachmentRequest = await AuthenticatedHttpRequestFactory(
                        method: HttpMethod.Post,
                        uri: GetUri("attachment"),
                        content: new StringContent(
                            JsonSerializer.Serialize(initializeAttachmentPayload),
                            new MediaTypeHeaderValue("application/json")
                        ),
                        payload: payload
                    );
                    var initializeAttachmentResponse = await HandleServerCommunication<string>(
                        initializeAttachmentRequest,
                        cancellationToken
                    );
                    var initializeAttachmentContent = initializeAttachmentResponse;
                    if (initializeAttachmentContent is null)
                    {
                        throw new CorrespondenceRequestException(
                            "Attachment initialization request did not return content.",
                            null,
                            HttpStatusCode.InternalServerError,
                            "No content returned from attachment initialization"
                        );
                    }
                    var attachmentId = initializeAttachmentContent.Trim('"');
                    HttpContent attachmentDataContent = new StreamContent(attachment.Data);
                    attachmentDataContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    attachmentDataContent.Headers.TryAddWithoutValidation("Transfer-Encoding", "Chunked");
                    var uploadAttachmentRequest = await AuthenticatedHttpRequestFactory(
                        method: HttpMethod.Post,
                        uri: GetUri($"attachment/{attachmentId}/upload"),
                        content: attachmentDataContent,
                        payload: payload
                    );
                    var uploadAttachmentResponse = await HandleServerCommunication<AttachmentOverview>(
                        uploadAttachmentRequest,
                        cancellationToken
                    );
                    if (Guid.TryParse(attachmentId, out var guidId))
                    {
                        premadeAttachments.Add(guidId);
                        pollingJobs.Add(PollAttachmentStatus(guidId, payload, cancellationToken));
                    }
                }
                await Task.WhenAll(pollingJobs);
                payload.CorrespondenceRequest.ExistingAttachments = premadeAttachments;
                payload.CorrespondenceRequest.Content.Attachments = [];
            }
            using MultipartFormDataContent content = payload.CorrespondenceRequest.Serialise();
            using HttpRequestMessage request = await AuthenticatedHttpRequestFactory(
                method: HttpMethod.Post,
                uri: GetUri("correspondence/upload"),
                content: content,
                payload: payload
            );

            var response = await HandleServerCommunication<SendCorrespondenceResponse>(request, cancellationToken);
            activity?.SetCorrespondence(response);
            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Success);

            return response;
        }
        catch (CorrespondenceException e)
        {
            var requestException = e as CorrespondenceRequestException;

            _logger.LogError(
                e,
                "Failed to send correspondence: status={StatusCode} response={Response}",
                requestException?.HttpStatusCode.ToString() ?? "Unknown",
                requestException?.ResponseBody ?? "No response body"
            );

            activity?.Errored(e, requestException?.ProblemDetails?.Detail);
            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Error);
            throw;
        }
        catch (Exception e)
        {
            activity?.Errored(e);
            _logger.LogError(e, "Failed to send correspondence: {Exception}", e);
            _telemetry?.RecordCorrespondenceOrder(CorrespondenceResult.Error);
            throw new CorrespondenceRequestException($"Failed to send correspondence: {e}", e);
        }
    }

    /// <inheritdoc />
    private async Task PollAttachmentStatus(
        Guid attachmentId,
        CorrespondencePayloadBase payload,
        CancellationToken cancellationToken
    )
    {
        const int maxAttempts = 3;
        const int delaySeconds = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

            var statusRequest = await AuthenticatedHttpRequestFactory(
                method: HttpMethod.Get,
                uri: GetUri($"attachment/{attachmentId}"),
                content: null,
                payload: payload
            );
            var statusResponse = await HandleServerCommunication<AttachmentOverview>(statusRequest, cancellationToken);

            if (statusResponse is null)
            {
                break;
            }
            if (statusResponse.Status == "Published")
            {
                return;
            }
        }
        throw new CorrespondenceRequestException(
            $"Failure when uploading attachment. Attachment was not published in time.",
            null,
            HttpStatusCode.InternalServerError,
            "Polling failed"
        );
    }

    /// <inheritdoc/>
    public async Task<GetCorrespondenceStatusResponse> GetStatus(
        GetCorrespondenceStatusPayload payload,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Fetching correspondence status for {CorrespondenceId}", payload.CorrespondenceId);
        using Activity? activity = _telemetry?.StartCorrespondenceStatusActivity(payload.CorrespondenceId);

        try
        {
            using HttpRequestMessage request = await AuthenticatedHttpRequestFactory(
                method: HttpMethod.Get,
                uri: GetUri($"correspondence/{payload.CorrespondenceId}/details"),
                content: null,
                payload: payload
            );

            return await HandleServerCommunication<GetCorrespondenceStatusResponse>(request, cancellationToken);
        }
        catch (CorrespondenceException e)
        {
            var requestException = e as CorrespondenceRequestException;

            _logger.LogError(
                e,
                "Failed to fetch correspondence status: status={StatusCode} response={Response}",
                requestException?.HttpStatusCode.ToString() ?? "Unknown",
                requestException?.ResponseBody ?? "No response body"
            );

            activity?.Errored(e, requestException?.ProblemDetails?.Detail);
            throw;
        }
        catch (Exception e)
        {
            activity?.Errored(e);
            _logger.LogError(e, "Failed to fetch correspondence status: {Exception}", e);
            throw new CorrespondenceRequestException($"Failed to fetch correspondence status: {e}", e);
        }
    }

    private async Task<HttpRequestMessage> AuthenticatedHttpRequestFactory(
        HttpMethod method,
        string uri,
        HttpContent? content,
        CorrespondencePayloadBase payload
    )
    {
        _logger.LogDebug("Fetching access token via factory");
        JwtToken accessToken = await _authorisationFactory.Resolve(payload);

        _logger.LogDebug("Constructing authorized http request for target uri {TargetEndpoint}", uri);
        HttpRequestMessage request = new(method, uri) { Content = content };

        request.Headers.Authorization = new AuthenticationHeaderValue(AuthorizationSchemes.Bearer, accessToken);
        request.Headers.TryAddWithoutValidation(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);

        return request;
    }

    private ValidationProblemDetails? GetProblemDetails(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseBody);
            if (problemDetails is null)
            {
                return null;
            }

            problemDetails.Detail ??=
                problemDetails.Errors.Count > 0
                    ? JsonSerializer.Serialize(problemDetails.Errors)
                    : $"Unknown error. Full server response: {responseBody}";

            return problemDetails;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error parsing ProblemDetails from Correspondence api");
        }

        return null;
    }

    private string GetUri(string relativePath)
    {
        string baseUri = _platformSettings.ApiCorrespondenceEndpoint.TrimEnd('/');
        return $"{baseUri}/{relativePath.TrimStart('/')}";
    }

    private async Task<TContent> HandleServerCommunication<TContent>(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using HttpClient client = _httpClientFactory.CreateClient();

        // Configure HttpClient for large file uploads
        client.Timeout = TimeSpan.FromMinutes(30);
        client.DefaultRequestHeaders.ExpectContinue = false;

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var problemDetails = GetProblemDetails(responseBody);
            throw new CorrespondenceRequestException(
                $"Correspondence request failed with status {response.StatusCode}: {problemDetails?.Detail}",
                problemDetails,
                response.StatusCode,
                responseBody
            );
        }

        _logger.LogDebug("Correspondence request succeeded: {Response}", responseBody);

        try
        {
            return JsonSerializer.Deserialize<TContent>(responseBody)
                ?? throw new CorrespondenceRequestException(
                    "Literal null content received from Correspondence API server"
                );
        }
        catch (Exception e)
        {
            throw new CorrespondenceRequestException(
                $"Invalid response from Correspondence API server: {responseBody}",
                null,
                response.StatusCode,
                responseBody,
                e
            );
        }
    }
}
