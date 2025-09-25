using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Altinn.App.Api.Models;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

// using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivInstanceClient : IFiksArkivInstanceClient
{
    private readonly IAuthenticationTokenResolver _authenticationTokenResolver;
    private readonly Telemetry? _telemetry;
    private readonly PlatformSettings _platformSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAppMetadata _appMetadata;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly GeneralSettings _generalSettings;
    private readonly ILogger<FiksArkivInstanceClient> _logger;

    private readonly AuthenticationMethod _serviceOwnerAuth = AuthenticationMethod.ServiceOwner();

    public FiksArkivInstanceClient(
        IOptions<PlatformSettings> platformSettings,
        IOptions<GeneralSettings> generalSettings,
        IAuthenticationTokenResolver authenticationTokenResolver,
        IHttpClientFactory httpClientFactory,
        IAppMetadata appMetadata,
        IAccessTokenGenerator accessTokenGenerator,
        ILogger<FiksArkivInstanceClient> logger,
        Telemetry? telemetry = null
    )
    {
        _platformSettings = platformSettings.Value;
        _generalSettings = generalSettings.Value;
        _telemetry = telemetry;
        _authenticationTokenResolver = authenticationTokenResolver;
        _httpClientFactory = httpClientFactory;
        _appMetadata = appMetadata;
        _accessTokenGenerator = accessTokenGenerator;
        _logger = logger;
    }

    public async Task<Instance> GetInstance(InstanceIdentifier instanceIdentifier)
    {
        using var activity = _telemetry?.StartGetInstanceByGuidActivity(instanceIdentifier.InstanceGuid);

        using HttpClient client = await GetAuthenticatedClient();
        using HttpResponseMessage response = await client.GetAsync($"instances/{instanceIdentifier}");

        return await DeserializeResponse<Instance>(response);
    }

    public async Task ProcessMoveNext(InstanceIdentifier instanceIdentifier, string? action = null)
    {
        using var activity = _telemetry?.StartApiProcessNextActivity(instanceIdentifier);

        try
        {
            using HttpClient client = await GetAuthenticatedClient();
            using StringContent actionPayload = GetProcessNextAction(action);
            using HttpResponseMessage response = await client.PutAsync(
                $"instances/{instanceIdentifier}/process/next",
                actionPayload
            );

            await EnsureSuccessStatusCode(response);

            _logger.LogInformation("Moved instance {InstanceId} to next step.", instanceIdentifier);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to move instance {InstanceId} to next step: {Error}", instanceIdentifier, e);
            throw;
        }
    }

    public async Task MarkInstanceComplete(InstanceIdentifier instanceIdentifier)
    {
        using var activity = _telemetry?.StartApiProcessCompleteActivity(instanceIdentifier);

        try
        {
            using HttpClient client = await GetAuthenticatedClient();
            using StringContent emptyPayload = new(string.Empty);
            using HttpResponseMessage response = await client.PostAsync(
                $"instances/{instanceIdentifier}/complete",
                emptyPayload
            );

            await EnsureSuccessStatusCode(response);

            _logger.LogInformation("Marked {InstanceId} as completed.", instanceIdentifier);
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to mark instance {InstanceId} as completed: {Error}", instanceIdentifier, e);
            throw;
        }
    }

    public async Task<DataElement> InsertBinaryData(
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
            if (string.IsNullOrWhiteSpace(dataType))
                throw new FiksArkivException("Data type cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(filename))
                throw new FiksArkivException("Filename cannot be null or empty.");
            if (contentType?.Contains('/') != true)
                throw new FiksArkivException("Content type must be a valid MIME type.");

            string url = $"instances/{instanceIdentifier}/data?dataType={dataType}";
            if (!string.IsNullOrEmpty(generatedFromTask))
                url += $"&generatedFromTask={generatedFromTask}";

            using StreamContent content = new(stream);
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

        T? deserializedContent;
        try
        {
            deserializedContent = JsonConvert.DeserializeObject<T>(content);
        }
        catch (Exception e)
        {
            throw new PlatformHttpException(
                response,
                $"Error deserializing JSON data: {e.Message}. The content was: {content}",
                e
            );
        }

        return deserializedContent ?? throw GetPlatformHttpException(response, content);
    }

    private static async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string content = await response.Content.ReadAsStringAsync();
        throw GetPlatformHttpException(response, content);
    }

    private static PlatformHttpException GetPlatformHttpException(
        HttpResponseMessage response,
        string content,
        Exception? innerException = null
    )
    {
        string errorMessage = $"{(int)response.StatusCode} {response.ReasonPhrase}: {content}";
        return new PlatformHttpException(response, errorMessage, innerException);
    }

    private async Task<HttpClient> GetAuthenticatedClient(string? bearerToken = null)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        bearerToken ??= await _authenticationTokenResolver.GetAccessToken(_serviceOwnerAuth);
        string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(appMetadata.AppIdentifier);

        HttpClient client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, _platformSettings.SubscriptionKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            AuthorizationSchemes.Bearer,
            bearerToken
        );
        client.DefaultRequestHeaders.Add(
            General.PlatformAccessTokenHeaderName,
            _accessTokenGenerator.GenerateAccessToken(appMetadata.AppIdentifier.Org, appMetadata.AppIdentifier.App)
        );

        return client;
    }

    internal static StringContent GetProcessNextAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return new StringContent(string.Empty);

        var payload = new ProcessNext { Action = action };

        return new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
    }
}
