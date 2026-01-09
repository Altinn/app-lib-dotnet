using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Dan;
using Altinn.App.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Altinn.App.Core.Infrastructure.Clients.Dan;

/// <summary>
/// Client for interacting with the Dan API.
/// </summary>
public class DanClient : IDanClient
{
    private HttpClient _httpClient;
    private IOptions<DanSettings> _settings;
    private IAuthenticationTokenResolver _authenticationTokenResolver;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="settings"></param>
    /// <param name="serviceProvider"></param>
    public DanClient(HttpClient httpClient, IOptions<DanSettings> settings, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _settings = settings;
        _authenticationTokenResolver = serviceProvider.GetRequiredService<IAuthenticationTokenResolver>();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
    }

    /// <summary>
    /// Returns dataset from Dan API.
    /// </summary>
    /// <param name="dataset">Dataset from Dan</param>
    /// <param name="subject">Usually ssn or orgNumber</param>
    /// <param name="jmesPath">jmesPath - usd to filter out just the fields we need</param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> GetDataset(string dataset, string subject, string fields)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        var token = await GetMaskinportenToken();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SetSubscriptionKey(dataset));

        var body = new { Subject = subject };
        var myContent = JsonConvert.SerializeObject(body);
        HttpContent content = new StringContent(myContent, Encoding.UTF8, "application/json");

        var fieldsToFill = GetQuery(fields);
        var baseQuery = $"{fieldsToFill.First()} : {fieldsToFill.First()}";
        foreach (var jsonKey in fieldsToFill.Skip(1))
        {
            baseQuery += $",{jsonKey} : {jsonKey}";
        }

        var query = "[].{" + baseQuery + "}||{" + baseQuery + "}";

        var result = await _httpClient.PostAsync(
            $"directharvest/{dataset}?envelope=false&requestor=991825827&query={query}",
            content
        );

        if (result.IsSuccessStatusCode)
        {
            var dictionary = new Dictionary<string, string>();
            var resultJson = result.Content.ReadAsStringAsync().Result;

            //some datasets might return an array. The array need to be serialized differently than a single object
            if (IsJsonArray(resultJson))
            {
                dictionary = await ConvertListToDictionary(resultJson);
            }
            else
            {
                dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultJson);
            }
            if (dictionary != null)
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

    private async Task<Dictionary<string, string>> ConvertListToDictionary(string jsonString)
    {
        var list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonString);

        var mergedDictionary = list.SelectMany(d => d)
            .GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => string.Join(",", g.Select(x => x.Value)));

        return mergedDictionary;
    }

    private bool IsJsonArray(string jsonString)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private List<string> GetQuery(string json)
    {
        var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);

        var keys = list.Where(l => l.Any()).Select(l => l.Keys.First()).ToList();
        return keys;
    }

    //Todo: remove after testing
    private string SetSubscriptionKey(string dataset)
    {
        switch (dataset.ToUpper())
        {
            case "UNITBASICINFORMATION":
                return _settings.Value.SubscriptionKey;
            case "SKIPSREGISTRENE":
                return _settings.Value.SubscriptionKeySkipsRegistrene;
            default:
                return "";
        }
    }
}
