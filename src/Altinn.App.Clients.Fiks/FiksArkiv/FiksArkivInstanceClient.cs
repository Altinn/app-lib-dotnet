using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivInstanceClient : IFiksArkivInstanceClient
{
    private readonly IMaskinportenClient _maskinportenClient;
    private readonly Telemetry? _telemetry;
    private readonly PlatformSettings _platformSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppMetadata _appMetadata;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly GeneralSettings _generalSettings;
    private readonly ILogger<FiksArkivInstanceClient> _logger;

    public FiksArkivInstanceClient(
        IOptions<PlatformSettings> platformSettings,
        IOptions<GeneralSettings> generalSettings,
        IMaskinportenClient maskinportenClient,
        IHttpClientFactory httpClientFactory,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IAccessTokenGenerator accessTokenGenerator,
        ILogger<FiksArkivInstanceClient> logger,
        Telemetry? telemetry = null
    )
    {
        _platformSettings = platformSettings.Value;
        _generalSettings = generalSettings.Value;
        _telemetry = telemetry;
        _maskinportenClient = maskinportenClient;
        _httpClientFactory = httpClientFactory;
        _appMetadata = appMetadata;
        _hostEnvironment = hostEnvironment;
        _accessTokenGenerator = accessTokenGenerator;
        _logger = logger;
    }

    public async Task<string> GetServiceOwnerAccessToken()
    {
        return _hostEnvironment.IsDevelopment()
            ? await GetLocaltestToken()
            : await _maskinportenClient.GetAltinnExchangedToken(
                ["altinn:serviceowner/instances.read", "altinn:serviceowner/instances.write"]
            );
    }

    public async Task<Instance> GetInstance(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier)
    {
        using var activity = _telemetry?.StartGetInstanceByGuidActivity(instanceIdentifier.InstanceGuid);

        using HttpClient client = await GetAuthenticatedClient();
        using HttpResponseMessage response = await client.GetAsync($"instances/{instanceIdentifier}");

        return await DeserializeResponse<Instance>(response);
    }

    public async Task ProcessMoveNext(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        string? action = null
    )
    {
        using var activity = _telemetry?.StartApiProcessNextActivity(instanceIdentifier);

        try
        {
            string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(appIdentifier);
            using HttpClient client = await GetAuthenticatedClient();
            using HttpResponseMessage response = await client.PutAsync(
                $"{baseUrl}instances/{instanceIdentifier}/process/next",
                GetProcessNextAction()
            );

            await EnsureSuccessStatusCode(response);

            _logger.LogInformation("Moved instance {instanceId} to next step.", instanceIdentifier);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to move instance {InstanceId} to next step: {Error}", instanceIdentifier, e);
            throw;
        }

        StringContent GetProcessNextAction()
        {
            if (string.IsNullOrWhiteSpace(action))
                return new StringContent(string.Empty);

            var payload = new { Action = action };

            return new StringContent(
                JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
                Encoding.UTF8,
                "application/json"
            );
        }
    }

    public async Task MarkInstanceComplete(AppIdentifier appIdentifier, InstanceIdentifier instanceIdentifier)
    {
        using var activity = _telemetry?.StartApiProcessCompleteActivity(instanceIdentifier);

        try
        {
            string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(appIdentifier);
            using HttpClient client = await GetAuthenticatedClient();
            using HttpResponseMessage response = await client.PostAsync(
                $"{baseUrl}instances/{instanceIdentifier}/complete",
                new StringContent(string.Empty)
            );

            await EnsureSuccessStatusCode(response);

            _logger.LogInformation("Marked {instanceId} as completed.", instanceIdentifier);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to mark instance {InstanceId} as completed: {Error}", instanceIdentifier, e);
            throw;
        }
    }

    public async Task<DataElement> InsertBinaryData(
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        string dataType,
        string contentType,
        string filename,
        Stream stream,
        string? generatedFromTask = null
    )
    {
        using var activity = _telemetry?.StartInsertBinaryDataActivity(instanceIdentifier.ToString());

        try
        {
            string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(appIdentifier);
            string url = $"{baseUrl}instances/{instanceIdentifier}/data?dataType={dataType}";
            if (!string.IsNullOrEmpty(generatedFromTask))
                url += $"&generatedFromTask={generatedFromTask}";

            StreamContent content = new(stream);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = filename,
                FileNameStar = filename,
            };

            using HttpClient client = await GetAuthenticatedClient();
            using HttpResponseMessage response = await client.PostAsync(url, content);

            return await DeserializeResponse<DataElement>(response);
        }
        catch (Exception e)
        {
            _logger.LogError("Error storing binary data for instance {InstanceId}: {Error}", instanceIdentifier, e);
            throw;
        }
    }

    private static async Task<T> DeserializeResponse<T>(HttpResponseMessage response)
    {
        await EnsureSuccessStatusCode(response);
        string content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(content) ?? throw GetPlatformHttpException(response, content);
    }

    private static async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string content = await response.Content.ReadAsStringAsync();
        throw GetPlatformHttpException(response, content);
    }

    private static PlatformHttpException GetPlatformHttpException(HttpResponseMessage response, string content)
    {
        string errorMessage = $"{(int)response.StatusCode} {response.ReasonPhrase}: {content}";
        return new PlatformHttpException(response, errorMessage);
    }

    private async Task<string> GetLocaltestToken()
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var url =
            $"http://localhost:5101/Home/GetTestOrgToken?org={appMetadata.Org}&orgNumber=991825827&authenticationLevel=3&scopes=altinn%3Aserviceowner%2Finstances.read+altinn%3Aserviceowner%2Finstances.write";

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url);

        await EnsureSuccessStatusCode(response);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> GetAuthenticatedClient(string? bearerToken = null)
    {
        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();
        string issuer = application.Org;
        string appName = application.AppIdentifier.App;
        bearerToken ??= await GetServiceOwnerAccessToken();

        HttpClient client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_platformSettings.ApiStorageEndpoint);
        client.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        client.DefaultRequestHeaders.Add(
            General.PlatformAccessTokenHeaderName,
            _accessTokenGenerator.GenerateAccessToken(issuer, appName)
        );

        return client;
    }
}
