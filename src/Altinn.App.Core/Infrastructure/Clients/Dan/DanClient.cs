using System.Net.Http.Headers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Dan;
using Altinn.App.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Altinn.App.Core.Features.Infrastructure.Clients.Dan;

public class DanClient : IDanClient
{
    private HttpClient _httpClient;
    private IOptions<DanSettings> _settings;
    private IAuthenticationTokenResolver _authenticationTokenResolver;

    public DanClient(HttpClient httpClient, IOptions<DanSettings> settings, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _settings = settings;
        _authenticationTokenResolver = serviceProvider.GetRequiredService<IAuthenticationTokenResolver>();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", settings.Value.SubscriptionKey);
        httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
    }

    public async Task<Dictionary<string, string>> GetDataset(string dataset, string subject)
    {
        var token = await GetMaskinportenToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        var result = await _httpClient.GetAsync(
            $"directharvest/{dataset}?subject={subject}&envelope=false&requestor=991825827"
        );
        if (result.IsSuccessStatusCode)
        {
            var resultJson = result.Content.ReadAsStringAsync().Result;
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultJson);
            return dictionary;
        }
        return new Dictionary<string, string>();
    }

    private async Task<JwtToken> GetMaskinportenToken()
    {
        var token = await _authenticationTokenResolver.GetAccessToken(
            AuthenticationMethod.Maskinporten(_settings.Value.Scope),
            CancellationToken.None
        );
        return token;
    }
}
