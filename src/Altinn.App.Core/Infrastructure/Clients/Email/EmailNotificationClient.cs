using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Email;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Email;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Altinn.App.Core.Infrastructure.Clients.Email;
/// <summary>
/// Implementation of the <see cref="IEmailNotificationClient"/> interface using a HttpClient to send
/// requests to the Email Notification service.
/// </summary>
public class EmailNotificationClient : IEmailNotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppMetadata _appMetadata;
    private readonly PlatformSettings _platformSettings;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly TelemetryClient _telemetryClient;
    private static readonly Counter _orderCount = Metrics
        .CreateCounter("altinn_app_notification_order_request_count", "Number of notification order requests.", labelNames: ["email"]);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    /// <exception cref="EmailNotificationException"/>
    public async Task<EmailOrderResponse> Order(EmailNotification emailNotification, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        var timer = Stopwatch.StartNew();

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        EmailOrderResponse? orderResponse = null;
        try
        {
            var requestContent = JsonSerializer.Serialize(emailNotification, _jsonSerializerOptions);
            using var stringContent = new StringContent(requestContent, Encoding.UTF8, "application/json");

            var uri = _platformSettings.NotificationEndpoint.TrimEnd('/') + "/api/v1/orders/email";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = stringContent,
                Method = HttpMethod.Post,
            };
            await AddAuthHeader(httpRequest);

            httpResponseMessage = await _httpClient.SendAsync(httpRequest, ct);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _orderCount.WithLabels("error").Inc();
                httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
                var ex = new EmailNotificationException("Email notification failed");
                ex.Data.Add("responseContent", httpContent);
                ex.Data.Add("responseStatusCode", httpResponseMessage.StatusCode.ToString());
                ex.Data.Add("responseReasonPhrase", httpResponseMessage.ReasonPhrase);

                throw ex;
            }
            httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);

            orderResponse = JsonSerializer.Deserialize<EmailOrderResponse>(httpContent);
            if (orderResponse is null)
                throw new Exception("Couldn't deserialize email notification order response");

            _orderCount.WithLabels("success").Inc();
            return orderResponse;
        }
        catch(Exception e) when (e is not EmailNotificationException)
        {
            _orderCount.WithLabels("error").Inc();
            var ex = new EmailNotificationException("Something went wrong when processing the email order, see inner exception for details.", e);
            ex.Data.Add("responseContent", httpContent);
            ex.Data.Add("responseStatusCode", httpResponseMessage?.StatusCode.ToString());
            ex.Data.Add("responseReasonPhrase", httpResponseMessage?.ReasonPhrase);
            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();

            timer.Stop();
            _telemetryClient.TrackDependency(
                "Altinn.Notifications",
                "OrderEmailNotification",
                "",
                startTime,
                timer.Elapsed,
                orderResponse is not null
            );
        }
    }

    private async Task AddAuthHeader(HttpRequestMessage request)
    {
        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();
        request.Headers.Add("PlatformAccessToken", _accessTokenGenerator.GenerateAccessToken(application.Org, application.AppIdentifier.App));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use in communication with the email notification service.</param>
    /// <param name="platformSettings">Api endpoints for platform services.</param>
    /// <param name="appMetadata">The service providing appmetadata.</param>
    /// <param name="accessTokenGenerator">An access token generator to create an access token.</param>
    /// <param name="telemetryClient">Client used to track dependencies.</param>
    public EmailNotificationClient(
        HttpClient httpClient,
        IOptions<PlatformSettings> platformSettings,
        IAppMetadata appMetadata,
        IAccessTokenGenerator accessTokenGenerator,
        TelemetryClient telemetryClient)
    {
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
        _appMetadata = appMetadata;
        _accessTokenGenerator = accessTokenGenerator;
        _telemetryClient = telemetryClient;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
