using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Dan;
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
    private readonly HttpClient _httpClient;
    private readonly IOptions<DanSettings> _settings;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="settings"></param>
    public DanClient(IHttpClientFactory factory, IOptions<DanSettings> settings)
    {
        _httpClient = factory.CreateClient("DanClient");
        _settings = settings;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
    }

    /// <summary>
    /// Returns dataset from Dan API.
    /// </summary>
    /// <param name="dataset">Dataset from Dan</param>
    /// <param name="subject">Usually ssn or orgNumber</param>
    /// <param name="fields">The fields we fetch from the api</param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> GetDataset(string dataset, string subject, string fields)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _settings.Value.SubscriptionKey);

        var body = new { Subject = subject };
        var myContent = JsonConvert.SerializeObject(body);
        HttpContent content = new StringContent(myContent, Encoding.UTF8, "application/json");

        var fieldsToFill = GetQuery(fields);
        var baseQuery = $"{fieldsToFill.First()} : {fieldsToFill.First()}";
        foreach (var jsonKey in fieldsToFill.Skip(1))
        {
            //if there is more than one field to fetch, add it to the query
            baseQuery += $",{jsonKey} : {jsonKey}";
        }

        //ensures that the query returns a list if endpoint returns a list and an object when endpoint returns a single object
        var query = "[].{" + baseQuery + "}||{" + baseQuery + "}";

        var result = await _httpClient.PostAsync(
            $"directharvest/{dataset}?envelope=false&reuseToken=true&query={query}",
            content
        );

        if (result.IsSuccessStatusCode)
        {
            var dictionary = new Dictionary<string, string>();
            var resultJson = result.Content.ReadAsStringAsync().Result;

            //some datasets might return an array. The array needs to be serialized differently than a single object
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

    private static Task<Dictionary<string, string>> ConvertListToDictionary(string jsonString)
    {
        var list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonString);
        if (list != null)
            return Task.FromResult(
                list.SelectMany(d => d)
                    .GroupBy(kvp => kvp.Key)
                    .ToDictionary(g => g.Key, g => string.Join(",", g.Select(x => x.Value)))
            );

        return Task.FromResult(new Dictionary<string, string>());
    }

    private static bool IsJsonArray(string jsonString)
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

    private static List<string> GetQuery(string json)
    {
        var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
        if (list != null)
            return list.Where(l => l.Count != 0).Select(l => l.Keys.First()).ToList();
        return new List<string>();
    }
}
