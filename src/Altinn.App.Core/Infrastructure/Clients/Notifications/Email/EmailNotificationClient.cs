using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Notifications.Email;
using Altinn.App.Core.Models.Notifications.Email;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Altinn.App.Core.Infrastructure.Clients.Notifications.Email;

internal sealed class EmailNotificationClient : IEmailNotificationClient
{
    private readonly ILogger<EmailNotificationClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppMetadata _appMetadata;
    private readonly PlatformSettings _platformSettings;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly TelemetryClient? _telemetryClient;

    public EmailNotificationClient(
        ILogger<EmailNotificationClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<PlatformSettings> platformSettings,
        IAppMetadata appMetadata,
        IAccessTokenGenerator accessTokenGenerator,
        TelemetryClient? telemetryClient = null)
    {
        _logger = logger;
        _platformSettings = platformSettings.Value;
        _httpClientFactory = httpClientFactory;
        _appMetadata = appMetadata;
        _accessTokenGenerator = accessTokenGenerator;
        _telemetryClient = telemetryClient;
    }

    public async Task<EmailOrderResponse> Order(EmailNotification emailNotification, CancellationToken ct)
    {
        using var dependency = new Telemetry.Dependency(_telemetryClient);

        using var httpClient = _httpClientFactory.CreateClient();

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        EmailOrderResponse? orderResponse = null;
        try
        {
            var application = await _appMetadata.GetApplicationMetadata();

            var uri = _platformSettings.NotificationEndpoint.TrimEnd('/') + "/api/v1/orders/email";
            var body = JsonSerializer.Serialize(emailNotification);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(body, new MediaTypeHeaderValue("application/json")),
            };
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequestMessage.Headers.Add(
                "PlatformAccessToken",
                _accessTokenGenerator.GenerateAccessToken(application.Org, application.AppIdentifier.App)
            );

            httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, ct);
            httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                orderResponse = JsonSerializer.Deserialize<EmailOrderResponse>(httpContent);
                if (orderResponse is null)
                    throw new JsonException("Couldn't deserialize email notification order response.");

                Telemetry.OrderCount.WithLabels(Telemetry.Types.Email, Telemetry.Result.Success).Inc();
            }
            else
            {
                throw new HttpRequestException("Got error status code for email notification order");
            }
            return orderResponse;
        }
        catch (Exception e)
        {
            dependency.Errored();
            Telemetry.OrderCount.WithLabels(Telemetry.Types.Email, Telemetry.Result.Error).Inc();
            var ex = new EmailNotificationException($"Something went wrong when processing the email order", httpResponseMessage, httpContent, e);
            _logger.LogError(ex, "Error when processing email notification order");
            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }
}
