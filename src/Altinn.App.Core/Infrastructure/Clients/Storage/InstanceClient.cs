using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

/// <summary>
/// A client for handling actions on instances in Altinn Platform.
/// </summary>
public class InstanceClient : IInstanceClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly Telemetry? _telemetry;
    private readonly IAuthenticationTokenResolver _authenticationTokenResolver;
    private readonly AuthenticationMethod _defaultAuthenticationMethod = StorageAuthenticationMethod.CurrentUser();

    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceClient"/> class.
    /// </summary>
    /// <param name="platformSettings">the platform settings</param>
    /// <param name="httpClient">A HttpClient that can be used to perform HTTP requests against the platform.</param>
    /// <param name="serviceProvider">The service provider</param>
    public InstanceClient(
        IOptions<PlatformSettings> platformSettings,
        HttpClient httpClient,
        IServiceProvider serviceProvider
    )
    {
        _authenticationTokenResolver = serviceProvider.GetRequiredService<IAuthenticationTokenResolver>();
        _logger = serviceProvider.GetRequiredService<ILogger<InstanceClient>>();
        _telemetry = serviceProvider.GetService<Telemetry>();

        httpClient.BaseAddress = new Uri(platformSettings.Value.ApiStorageEndpoint);
        httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        _client = httpClient;
    }

    /// <inheritdoc />
    public async Task<Instance> GetInstance(
        string app,
        string org,
        int instanceOwnerPartyId,
        Guid instanceId,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartGetInstanceByGuidActivity(instanceId);
        string instanceIdentifier = $"{instanceOwnerPartyId}/{instanceId}";
        string apiUrl = $"instances/{instanceIdentifier}";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.GetAsync(token, apiUrl);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return instance;
        }
        else
        {
            _logger.LogError("Unable to fetch instance with instance id {InstanceId}", instanceId);
            throw await PlatformHttpException.CreateAsync(response);
        }
    }

    /// <inheritdoc />
    public async Task<Instance> GetInstance(
        Instance instance,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
        using var activity = _telemetry?.StartGetInstanceByInstanceActivity(instanceGuid);
        string app = instance.AppId.Split("/")[1];
        string org = instance.Org;
        int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId, CultureInfo.InvariantCulture);

        return await GetInstance(app, org, instanceOwnerPartyId, instanceGuid, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Instance>> GetInstances(
        Dictionary<string, StringValues> queryParams,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartGetInstancesActivity();
        var apiUrl = QueryHelpers.AddQueryString("instances", queryParams);

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        QueryResponse<Instance> queryResponse = await QueryInstances(token, apiUrl);

        if (queryResponse.Count == 0)
        {
            return [];
        }
        List<Instance> instances = [.. queryResponse.Instances];

        while (!string.IsNullOrEmpty(queryResponse.Next))
        {
            queryResponse = await QueryInstances(token, queryResponse.Next);
            instances.AddRange(queryResponse.Instances);
        }
        return instances;
    }

    private async Task<QueryResponse<Instance>> QueryInstances(string token, string url)
    {
        using var activity = _telemetry?.StartQueryInstancesActivity();
        HttpResponseMessage response = await _client.GetAsync(token, url);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string responseString = await response.Content.ReadAsStringAsync();
            QueryResponse<Instance> queryResponse =
                JsonConvert.DeserializeObject<QueryResponse<Instance>>(responseString)
                ?? throw new JsonException("Could not deserialize Instance query response");
            return queryResponse;
        }
        else
        {
            _logger.LogError("Unable to query instances from Platform Storage");
            throw await PlatformHttpException.CreateAsync(response);
        }
    }

    /// <inheritdoc />
    public async Task<Instance> UpdateProcess(
        Instance instance,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdateProcessActivity(instance);
        ProcessState processState = instance.Process;
        string apiUrl = $"instances/{instance.Id}/process";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        string processStateString = JsonConvert.SerializeObject(processState);
        _logger.LogInformation("update process state: {ProcessStateString}", processStateString);

        StringContent httpContent = new(processStateString, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync(token, apiUrl, httpContent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance updatedInstance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return updatedInstance;
        }
        else
        {
            _logger.LogError("Unable to update instance process with instance id {InstanceId}", instance.Id);
            throw await PlatformHttpException.CreateAsync(response);
        }
    }

    /// <inheritdoc />
    public async Task<Instance> UpdateProcessAndEvents(
        Instance instance,
        List<InstanceEvent> events,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdateProcessActivity(instance, events.Count);
        ProcessState processState = instance.Process;

        foreach (var instanceEvent in events)
            instanceEvent.InstanceId = instance.Id;

        string apiUrl = $"instances/{instance.Id}/process/instanceandevents";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        var update = new ProcessStateUpdate { State = processState, Events = events };
        string updateString = JsonConvert.SerializeObject(update);
        _logger.LogInformation("update process state: {UpdateString}", updateString);

        StringContent httpContent = new(updateString, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PutAsync(token, apiUrl, httpContent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            Instance updatedInstance =
                JsonConvert.DeserializeObject<Instance>(instanceData)
                ?? throw new JsonException("Could not deserialize instance");
            return updatedInstance;
        }
        else
        {
            _logger.LogError("Unable to update instance process with instance id {InstanceId}", instance.Id);
            throw await PlatformHttpException.CreateAsync(response);
        }
    }

    /// <inheritdoc/>
    public async Task<Instance> CreateInstance(
        string org,
        string app,
        Instance instanceTemplate,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartCreateInstanceActivity();
        string apiUrl = $"instances?appId={org}/{app}";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        StringContent content = new(JsonConvert.SerializeObject(instanceTemplate), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PostAsync(token, apiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance createdInstance = JsonConvert.DeserializeObject<Instance>(
                await response.Content.ReadAsStringAsync(cancellationToken)
            )!;
            _telemetry?.InstanceCreated(createdInstance);
            return createdInstance;
        }

        _logger.LogError(
            "Unable to create instance {StatusCode} - {Response}",
            response.StatusCode,
            await response.Content.ReadAsStringAsync(cancellationToken)
        );
        throw await PlatformHttpException.CreateAsync(response);
    }

    /// <inheritdoc/>
    public async Task<Instance> AddCompleteConfirmation(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartCompleteConfirmationActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/complete";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.PostAsync(token, apiUrl, new StringContent(string.Empty));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            _telemetry?.InstanceCompleted(instance);
            return instance;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }

    /// <inheritdoc/>
    public async Task<Instance> UpdateReadStatus(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        string readStatus,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdateReadStatusActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/readstatus?status={readStatus}";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.PutAsync(token, apiUrl, new StringContent(string.Empty));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return instance;
        }

        _logger.LogError(
            "Could not update read status for instance {InstanceOwnerPartyId}/{InstanceGuid}. Request failed with status code {StatusCode}",
            instanceOwnerPartyId,
            instanceGuid,
            response.StatusCode
        );
#nullable disable
        return null;
#nullable restore
    }

    /// <inheritdoc/>
    public async Task<Instance> UpdateSubstatus(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        Substatus substatus,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdateSubStatusActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/substatus";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.PutAsync(
            token,
            apiUrl,
            new StringContent(JsonConvert.SerializeObject(substatus), Encoding.UTF8, "application/json")
        );

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return instance;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }

    /// <inheritdoc />
    public async Task<Instance> UpdatePresentationTexts(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        PresentationTexts presentationTexts,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdatePresentationTextActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/presentationtexts";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.PutAsync(
            token,
            apiUrl,
            new StringContent(JsonConvert.SerializeObject(presentationTexts), Encoding.UTF8, "application/json")
        );

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return instance;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }

    /// <inheritdoc />
    public async Task<Instance> UpdateDataValues(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        DataValues dataValues,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartUpdateDataValuesActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/datavalues";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.PutAsync(
            token,
            apiUrl,
            new StringContent(JsonConvert.SerializeObject(dataValues), Encoding.UTF8, "application/json")
        );

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            return instance;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }

    /// <inheritdoc />
    public async Task<Instance> DeleteInstance(
        int instanceOwnerPartyId,
        Guid instanceGuid,
        bool hard,
        StorageAuthenticationMethod? authenticationMethod = null,
        CancellationToken cancellationToken = default
    )
    {
        using var activity = _telemetry?.StartDeleteInstanceActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}?hard={hard}";

        JwtToken token = await _authenticationTokenResolver.GetAccessToken(
            authenticationMethod ?? _defaultAuthenticationMethod,
            cancellationToken: cancellationToken
        );

        HttpResponseMessage response = await _client.DeleteAsync(token, apiUrl);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            string instanceData = await response.Content.ReadAsStringAsync(cancellationToken);
            // ! TODO: this null-forgiving operator should be fixed/removed for the next major release
            Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
            _telemetry?.InstanceDeleted(instance);
            return instance;
        }

        throw await PlatformHttpException.CreateAsync(response);
    }
}
