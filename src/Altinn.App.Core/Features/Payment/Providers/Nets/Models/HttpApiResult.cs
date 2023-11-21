using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace Altinn.App.Core.Features.Payment.Providers.Nets.Models;


public class HttpApiResult<T>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    [MemberNotNullWhen(true, nameof(Success))]
    public bool IsSuccess => Success is not null;
    public  T? Success { get; init; }
    public HttpStatusCode Status { get; set; }
    public string? RawError { get; init; }

    public static async Task<HttpApiResult<T>> FromHttpResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                return new HttpApiResult<T>
                {
                    Status = response.StatusCode,
                    Success = await response.Content.ReadFromJsonAsync<T>(JSON_OPTIONS) ?? throw new JsonException("Could not deserialize response"),
                };
            }
            catch (JsonException e)
            {
                return new HttpApiResult<T>()
                {
                    Status = response.StatusCode,
                    RawError = e.Message,
                };
            }
        }

        return new()
            {
                Status = response.StatusCode,
                RawError = await response.Content.ReadAsStringAsync(),
        };
    }
}