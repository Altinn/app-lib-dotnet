﻿#nullable enable

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Register
{
    /// <summary>
    /// Represents an implementation of <see cref="IPersonRetriever"/> that will call the Register
    /// component to retrieve person information.
    /// </summary>
    public class PersonClient : IPersonRetriever
    {
        private readonly HttpClient _httpClient;
        private readonly IAppMetadata _appMetadata;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IUserTokenProvider _userTokenProvider;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient to be used to send requests to Register.</param>
        /// <param name="platformSettings">The platform settings from loaded configuration.</param>
        /// <param name="accessTokenGenerator">An access token generator to create an access token.</param>
        /// <param name="userTokenProvider">A service that can obtain the user JWT token.</param>
        /// <param name="appMetadata">The service providing appmetadata</param>
        public PersonClient(
            HttpClient httpClient,
            IOptions<PlatformSettings> platformSettings,
            IAppMetadata appMetadata,
            IAccessTokenGenerator accessTokenGenerator,
            IUserTokenProvider userTokenProvider)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(platformSettings.Value.ApiRegisterEndpoint);
            _httpClient.DefaultRequestHeaders.Add(
                General.SubscriptionKeyHeaderName,
                platformSettings.Value.SubscriptionKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _accessTokenGenerator = accessTokenGenerator;
            _userTokenProvider = userTokenProvider;
            _appMetadata = appMetadata;
        }

        /// <inheritdoc/>
        public async Task<Person?> GetPerson(string nationalIdentityNumber, string lastName, CancellationToken ct)
        {
            using(var request = new HttpRequestMessage(HttpMethod.Get, $"persons"))
            {
                await AddAuthHeaders(request);

                request.Headers.Add("X-Ai-NationalIdentityNumber", nationalIdentityNumber);
                request.Headers.Add("X-Ai-LastName", ConvertToBase64(lastName));

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);

                return await ReadResponse(response, ct);
            }
        }

        private async Task AddAuthHeaders(HttpRequestMessage request)
        {
            Application? application = await _appMetadata.GetApplicationMetadata();
            string? issuer = application?.Org;
            string? appName = application?.Id.Split("/")[1];
            request.Headers.Add(
                "PlatformAccessToken", _accessTokenGenerator.GenerateAccessToken(issuer, appName));
            request.Headers.Add(
                "Authorization", "Bearer " + _userTokenProvider.GetUserToken());
        }

        private async Task<Person?> ReadResponse(HttpResponseMessage response, CancellationToken ct)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadFromJsonAsync<Person>(_jsonSerializerOptions, ct);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw await PlatformHttpException.CreateAsync(response);
        }

        private static string ConvertToBase64(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }
    }
}
