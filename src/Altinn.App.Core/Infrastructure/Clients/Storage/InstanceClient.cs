using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

using AltinnCore.Authentication.Utils;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json;
using static Altinn.App.Core.Features.Telemetry.Instance;

namespace Altinn.App.Core.Infrastructure.Clients.Storage
{
    /// <summary>
    /// A client for handling actions on instances in Altinn Platform.
    /// </summary>
    public class InstanceClient : IInstanceClient
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _client;
        private readonly Telemetry _telemetry;
        private readonly AppSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceClient"/> class.
        /// </summary>
        /// <param name="platformSettings">the platform settings</param>
        /// <param name="logger">the logger</param>
        /// <param name="httpContextAccessor">The http context accessor </param>
        /// <param name="httpClient">A HttpClient that can be used to perform HTTP requests against the platform.</param>
        /// <param name="settings">The application settings.</param>
        /// <param name="telemetry">Telemetry for traces and metrics.</param>
        public InstanceClient(
            IOptions<PlatformSettings> platformSettings,
            ILogger<InstanceClient> logger,
            IHttpContextAccessor httpContextAccessor,
            HttpClient httpClient,
            IOptionsMonitor<AppSettings> settings,
            Telemetry telemetry)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _settings = settings.CurrentValue;
            httpClient.BaseAddress = new Uri(platformSettings.Value.ApiStorageEndpoint);
            httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            _client = httpClient;
            _telemetry = telemetry;
        }

        /// <inheritdoc />
        public async Task<Instance> GetInstance(string app, string org, int instanceOwnerPartyId, Guid instanceGuid)
        {
            using var activity = _telemetry.StartGetInstanceActivity(InstanceType.GetInstanceByGuid, instanceGuid);
            string instanceIdentifier = $"{instanceOwnerPartyId}/{instanceGuid}";

            string apiUrl = $"instances/{instanceIdentifier}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.GetAsync(token, apiUrl);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }
            else
            {
                _logger.LogError($"Unable to fetch instance with instance id {instanceGuid}");
                throw await PlatformHttpException.CreateAsync(response);
            }
        }

        /// <inheritdoc />
        public async Task<Instance> GetInstance(Instance instance)
        {
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            using var activity = _telemetry.StartGetInstanceActivity(InstanceType.GetInstanceByInstance, instanceGuid);
            string app = instance.AppId.Split("/")[1];
            string org = instance.Org;
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);

            return await GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        }

        /// <inheritdoc />
        public async Task<List<Instance>> GetInstances(Dictionary<string, StringValues> queryParams)
        {
            _telemetry.StartGetInstanceActivity(InstanceType.GetInstances);
            StringBuilder apiUrl = new($"instances?");

            foreach (var queryParameter in queryParams)
            {
                foreach (string value in queryParameter.Value)
                {
                    apiUrl.Append($"&{queryParameter.Key}={value}");
                }
            }

            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);
            QueryResponse<Instance> queryResponse = await QueryInstances(token, apiUrl.ToString());

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
            _telemetry.StartQueryInstancesActivity(InstanceType.QueryInstances, token);
            HttpResponseMessage response = await _client.GetAsync(token, url);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                QueryResponse<Instance> queryResponse = JsonConvert.DeserializeObject<QueryResponse<Instance>>(responseString)!;
                return queryResponse;
            }
            else
            {
                _logger.LogError("Unable to query instances from Platform Storage");
                throw await PlatformHttpException.CreateAsync(response);
            }
        }

        /// <inheritdoc />
        public async Task<Instance> UpdateProcess(Instance instance)
        {
            _telemetry.StartUpdateProcessActivity(InstanceType.UpdateProcess);
            ProcessState processState = instance.Process;

            string apiUrl = $"instances/{instance.Id}/process";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            string processStateString = JsonConvert.SerializeObject(processState);
            _logger.LogInformation($"update process state: {processStateString}");

            StringContent httpContent = new(processStateString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PutAsync(token, apiUrl, httpContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance updatedInstance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return updatedInstance;
            }
            else
            {
                _logger.LogError($"Unable to update instance process with instance id {instance.Id}");
                throw await PlatformHttpException.CreateAsync(response);
            }
        }

        /// <inheritdoc/>
        public async Task<Instance> CreateInstance(string org, string app, Instance instanceTemplate)
        {
            _telemetry.StartCreateInstanceActivity(InstanceType.CreateInstance);
            string apiUrl = $"instances?appId={org}/{app}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            StringContent content = new(JsonConvert.SerializeObject(instanceTemplate), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync(token, apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Instance createdInstance = JsonConvert.DeserializeObject<Instance>(await response.Content.ReadAsStringAsync())!;
                return createdInstance;
            }

            _logger.LogError($"Unable to create instance {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            throw await PlatformHttpException.CreateAsync(response);
        }

        /// <inheritdoc/>
        public async Task<Instance> AddCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid)
        {
            _telemetry.StartCompleteConfirmationActivity(InstanceType.CompleteConfirmation, instanceGuid);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/complete";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.PostAsync(token, apiUrl, new StringContent(string.Empty));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }

        /// <inheritdoc/>
        public async Task<Instance> UpdateReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus)
        {
            _telemetry.StartUpdateReadStatusActivity(InstanceType.UpdateReadStatus, instanceGuid, instanceOwnerPartyId);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/readstatus?status={readStatus}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.PutAsync(token, apiUrl, new StringContent(string.Empty));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            _logger.LogError($"Could not update read status for instance {instanceOwnerPartyId}/{instanceGuid}. Request failed with status code {response.StatusCode}");
            return null;
        }

        /// <inheritdoc/>
        public async Task<Instance> UpdateSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus)
        {
            _telemetry.StartUpdateSubStatusActivity(InstanceType.UpdateSubStatus, instanceGuid, instanceOwnerPartyId);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/substatus";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.PutAsync(token, apiUrl, new StringContent(JsonConvert.SerializeObject(substatus), Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }

        /// <inheritdoc />
        public async Task<Instance> UpdatePresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts)
        {
            _telemetry.StartUpdatePresentationTextActivity(InstanceType.UpdatePresentationText, instanceGuid, instanceOwnerPartyId);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/presentationtexts";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.PutAsync(token, apiUrl, new StringContent(JsonConvert.SerializeObject(presentationTexts), Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }

        /// <inheritdoc />
        public async Task<Instance> UpdateDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues)
        {
            _telemetry.StartUpdateDataValuesActivity(InstanceType.UpdateDataValues, instanceGuid, instanceOwnerPartyId);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/datavalues";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.PutAsync(token, apiUrl, new StringContent(JsonConvert.SerializeObject(dataValues), Encoding.UTF8, "application/json"));

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }

        /// <inheritdoc />
        public async Task<Instance> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hard)
        {
            _telemetry.StartDeleteInstanceActivity(InstanceType.DeleteInstance, instanceGuid, instanceOwnerPartyId);
            string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}?hard={hard}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _settings.RuntimeCookieName);

            HttpResponseMessage response = await _client.DeleteAsync(token, apiUrl);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string instanceData = await response.Content.ReadAsStringAsync();
                Instance instance = JsonConvert.DeserializeObject<Instance>(instanceData)!;
                return instance;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }
    }
}
