using System.Net.Http.Headers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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

    public FiksArkivInstanceClient(
        IOptions<PlatformSettings> platformSettings,
        IMaskinportenClient maskinportenClient,
        IHttpClientFactory httpClientFactory,
        IAppMetadata appMetadata,
        IHostEnvironment hostEnvironment,
        IAccessTokenGenerator accessTokenGenerator,
        Telemetry? telemetry = null
    )
    {
        _telemetry = telemetry;
        _maskinportenClient = maskinportenClient;
        _platformSettings = platformSettings.Value;
        _httpClientFactory = httpClientFactory;
        _appMetadata = appMetadata;
        _hostEnvironment = hostEnvironment;
        _accessTokenGenerator = accessTokenGenerator;
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

        string token = await GetServiceOwnerAccessToken();
        using HttpClient client = await GetAuthenticatedClient(token);
        using HttpResponseMessage response = await client.GetAsync($"instances/{instanceIdentifier}");
        response.EnsureSuccessStatusCode();

        string instanceData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Instance>(instanceData)
            ?? throw new InvalidOperationException(
                $"Unable to deserialize instance with instance id {instanceIdentifier}"
            );
    }

    private async Task<string> GetLocaltestToken()
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var url =
            $"http://localhost:5101/Home/GetTestOrgToken?org={appMetadata.Org}&authenticationLevel=3&scopes=altinn%3Aserviceowner%2Finstances.read+altinn%3Aserviceowner%2Finstances.write";

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpClient> GetAuthenticatedClient(string bearerToken)
    {
        ApplicationMetadata application = await _appMetadata.GetApplicationMetadata();
        string issuer = application.Org;
        string appName = application.AppIdentifier.App;

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
