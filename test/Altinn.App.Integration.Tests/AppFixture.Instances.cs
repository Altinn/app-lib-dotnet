using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Api.Models;

namespace Altinn.App.Integration.Tests;

public partial class AppFixture : IAsyncDisposable
{
    private InstancesOperations? _instances;
    internal InstancesOperations Instances
    {
        get
        {
            if (_instances == null)
            {
                _instances = new InstancesOperations(this);
            }
            return _instances;
        }
    }

    internal sealed class InstancesOperations(AppFixture fixture)
    {
        private readonly AppFixture _fixture = fixture;

        public async Task<ApiResponse> PostSimplified(string token, InstansiationInstance instansiation)
        {
            var client = _fixture.GetAppClient();
            var endpoint = $"/ttd/{_fixture._app}/instances/create";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = JsonSerializer.Serialize(instansiation, _jsonSerializerOptions);
            request.Content = new StringContent(payload, new MediaTypeHeaderValue("application/json"));
            var response = await client.SendAsync(request);
            return new ApiResponse(_fixture, response);
        }
    }
}
