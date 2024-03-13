using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Email;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Email;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.Extensions.Options;
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
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    /// <exception cref="EmailNotificationException"/>
    public async Task<EmailOrderResponse> Order(EmailNotification emailNotification, CancellationToken ct)
    {
        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
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
                httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
                var ex = new EmailNotificationException("Email notification failed");
                ex.Data.Add("responseContent", httpContent);
                ex.Data.Add("responseStatusCode", httpResponseMessage.StatusCode.ToString());
                ex.Data.Add("responseReasonPhrase", httpResponseMessage.ReasonPhrase);

                throw ex;
            }
            httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);

            var orderResponse = JsonSerializer.Deserialize<EmailOrderResponse>(httpContent);
            return orderResponse!;
        }
        catch(Exception e) when (e is not EmailNotificationException)
        {
            var ex = new EmailNotificationException("Email notification failed", e);
            ex.Data.Add("responseContent", httpContent);
            ex.Data.Add("responseStatusCode", httpResponseMessage?.StatusCode.ToString());
            ex.Data.Add("responseReasonPhrase", httpResponseMessage?.ReasonPhrase);
            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();
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
    public EmailNotificationClient(
        HttpClient httpClient,
        IOptions<PlatformSettings> platformSettings,
        IAppMetadata appMetadata,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
        _appMetadata = appMetadata;
        _accessTokenGenerator = accessTokenGenerator;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}
