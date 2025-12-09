using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Dan;
using Altinn.App.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Rest;

namespace Altinn.App.Core.Features.Infrastructure.Clients.Dan;

public class DanClient : IDanClient
{
    private HttpClient _httpClient;
    private IOptions<DanSettings> _settings;
    private IAuthenticationTokenResolver _authenticationTokenResolver;

    public DanClient(HttpClient httpClient, IOptions<DanSettings> settings, IServiceProvider serviceProvider)
    {
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

        var result = await _httpClient.GetAsync($"datasets/{dataset}?subject={subject}&envelope=false");
        var resultJson = result.Content.ReadFromJsonAsync<string>();
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
