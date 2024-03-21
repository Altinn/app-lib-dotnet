using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Notifications.Sms;
using Altinn.App.Core.Models.Notifications.Sms;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Notifications.Sms;

internal sealed class SmsNotificationClient : ISmsNotificationClient
{
    private readonly ILogger<SmsNotificationClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlatformSettings _platformSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly TelemetryClient? _telemetryClient;

    public SmsNotificationClient(
        ILogger<SmsNotificationClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<PlatformSettings> platformSettings,
        IAppMetadata appMetadata,
        IAccessTokenGenerator accessTokenGenerator,
        TelemetryClient? telemetryClient = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _platformSettings = platformSettings.Value;
        _appMetadata = appMetadata;
        _accessTokenGenerator = accessTokenGenerator;
        _telemetryClient = telemetryClient;
    }

    public async Task<SmsNotificationOrderResponse> Order(SmsNotification smsNotification, CancellationToken ct)
    {
        using var dependency = new Telemetry.Dependency(_telemetryClient);

        using var httpClient = _httpClientFactory.CreateClient();

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        Models.ApplicationMetadata? application = null;
        SmsNotificationOrderResponse? orderResponse = null;

        try
        {
            application = await _appMetadata.GetApplicationMetadata();

            var uri = _platformSettings.NotificationEndpoint.TrimEnd('/') + "/api/v1/orders/sms";
            var body = JsonSerializer.Serialize(smsNotification);

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
                orderResponse = JsonSerializer.Deserialize<SmsNotificationOrderResponse>(httpContent);
                if (orderResponse is null)
                    throw new Exception("Couldn't deserialize SMS notification order response");

                Telemetry.OrderCount.WithLabels(Telemetry.Types.Sms, Telemetry.Result.Success).Inc();
                return orderResponse;
            }
            else
            {
                throw new Exception("Got error status code for SMS notification order");
            }
        }
        catch (Exception e)
        {
            dependency.Errored();
            Telemetry.OrderCount.WithLabels(Telemetry.Types.Sms, Telemetry.Result.Error).Inc();
            var ex = new SmsNotificationException($"Something went wrong when processing the SMS notification order", httpResponseMessage, httpContent, e);
            _logger.LogError(ex, "Error when processing email notification order");
            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }
}
