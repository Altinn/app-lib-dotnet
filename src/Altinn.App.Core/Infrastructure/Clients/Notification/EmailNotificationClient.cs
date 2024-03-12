using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Email;
using Altinn.App.Core.Models.Email;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Altinn.App.Core.Infrastructure.Clients.Notification;
/// <summary>
/// Implementation of the <see cref="IEmailNotificationClient"/> interface using a HttpClient to send
/// requests to the Email Notification service.
/// </summary>
public class EmailNotificationClient : IEmailNotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly PlatformSettings _platformSettings;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    /// <exception cref="EmailNotificationException"/>
    public async Task<string> RequestEmailNotification(string url, EmailNotification emailNotification, CancellationToken ct)
    {
        string requestContent = JsonSerializer.Serialize(emailNotification, _jsonSerializerOptions);
        using StringContent stringContent = new(requestContent, Encoding.UTF8, "application/json");

        var httpResponseMessage = await _httpClient.PostAsync(_platformSettings.NotificationEndpoint, stringContent, ct);

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            var content = await httpResponseMessage.Content.ReadAsStringAsync(ct);
            var ex = new EmailNotificationException("Email notification failed");
            ex.Data.Add("responseContent", content);
            ex.Data.Add("responseStatusCode", httpResponseMessage.StatusCode.ToString());
            ex.Data.Add("responseReasonPhrase", httpResponseMessage.ReasonPhrase);

            throw ex;
        }
        return await httpResponseMessage.Content.ReadAsStringAsync(ct); // TODO: Deserialize into an object
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotificationClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use in communication with the email notification service.</param>
    /// <param name="platformSettings">Api endpoints for platform services.</param>
    public EmailNotificationClient(HttpClient httpClient, IOptions<PlatformSettings> platformSettings)
    {
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
    }
}
