using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Events;

/// <summary>
/// A client for handling actions on events in Altinn Platform.
/// </summary>
public class EventsClient : IEventsClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSettings _settings;
    private readonly GeneralSettings _generalSettings;
    private readonly HttpClient _client;
    private readonly Telemetry? _telemetry;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IAppMetadata _appMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsClient"/> class.
    /// </summary>
    /// <param name="platformSettings">The platform settings.</param>
    /// <param name="httpContextAccessor">The http context accessor.</param>
    /// <param name="httpClient">A HttpClient.</param>
    /// <param name="accessTokenGenerator">The access token generator service.</param>
    /// <param name="appMetadata">The app metadata service</param>
    /// <param name="settings">The application settings.</param>
    /// <param name="generalSettings">The general settings of the application.</param>
    /// <param name="telemetry">Telemetry for metrics and traces.</param>
    public EventsClient(
        IOptions<PlatformSettings> platformSettings,
        IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient,
        IAccessTokenGenerator accessTokenGenerator,
        IAppMetadata appMetadata,
        IOptionsMonitor<AppSettings> settings,
        IOptions<GeneralSettings> generalSettings,
        Telemetry? telemetry = null
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.CurrentValue;
        _generalSettings = generalSettings.Value;
        _accessTokenGenerator = accessTokenGenerator;
        _appMetadata = appMetadata;
        httpClient.BaseAddress = new Uri(platformSettings.Value.ApiEventsEndpoint);
        httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client = httpClient;
        _telemetry = telemetry;
    }

    /// <inheritdoc/>
    public async Task<string> AddEvent(string eventType, Instance instance)
    {
        using var activity = _telemetry?.StartAddEventActivity(instance);
        string? alternativeSubject = null;
        if (!string.IsNullOrWhiteSpace(instance.InstanceOwner.OrganisationNumber))
        {
            alternativeSubject = $"/org/{instance.InstanceOwner.OrganisationNumber}";
        }

        if (!string.IsNullOrWhiteSpace(instance.InstanceOwner.PersonNumber))
        {
            alternativeSubject = $"/person/{instance.InstanceOwner.PersonNumber}";
        }

        var baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance));

        CloudEvent cloudEvent = new CloudEvent
        {
            Subject = $"/party/{instance.InstanceOwner.PartyId}",
            Type = eventType,
            AlternativeSubject = alternativeSubject,
            Time = DateTime.UtcNow,
            SpecVersion = "1.0",
            Source = new Uri($"{baseUrl}instances/{instance.Id}"),
        };
        Application app = await _appMetadata.GetApplicationMetadata();
        string accessToken = _accessTokenGenerator.GenerateAccessToken(app?.Org, app?.Id.Split("/")[1]);

        string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

        string serializedCloudEvent = JsonSerializer.Serialize(cloudEvent);

        HttpResponseMessage response = await _client.PostAsync(
            token,
            "app",
            new StringContent(serializedCloudEvent, Encoding.UTF8, "application/json"),
            accessToken
        );

        if (response.IsSuccessStatusCode)
        {
            string eventId = await response.Content.ReadAsStringAsync();
            return eventId;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }
}
