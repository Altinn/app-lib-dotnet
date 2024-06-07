using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using JsonWebKeyConverter = Altinn.App.Core.Features.Maskinporten.Converters.JsonWebKeyConverter;

namespace Altinn.App.Core.Features.Maskinporten.Models;

/// <summary>
/// <para>
/// A configuration object that represents all required Maskinporten authentication settings.
/// </para>
/// <para>
/// Typically serialized as `maskinporten-settings.json` and injected in the runtime.
/// </para>
/// </summary>
public sealed record MaskinportenSettings
{
    /// <summary>
    /// The Maskinporten authority/audience to use for authentication and authorization.
    /// More info about environments and URIs <a href="https://docs.digdir.no/docs/Maskinporten/maskinporten_func_wellknown">in the docs</a>.
    /// </summary>
    [Required]
    [JsonPropertyName("authority")]
    public required string Authority { get; set; }

    /// <summary>
    /// The client ID which has been registered with Maskinporten. Typically a uuid4 string.
    /// </summary>
    [Required]
    [JsonPropertyName("clientId")]
    public required string ClientId { get; set; }

    /// <summary>
    /// The private key used to authenticate with Maskinporten, in JWK format.
    /// </summary>
    [Required]
    [JsonPropertyName("key")]
    [JsonConverter(typeof(JsonWebKeyConverter))]
    public required JsonWebKey Key { get; set; }
}

/// <summary>
/// Serialization wrapper for a JsonWebKey object
/// </summary>
internal readonly record struct JwkWrapper
{
    /// <summary>
    /// Key type
    /// </summary>
    [JsonPropertyName("kty")]
    public string? Kty { get; init; }

    /// <summary>
    /// Public key usage
    /// </summary>
    [JsonPropertyName("use")]
    public string? Use { get; init; }

    /// <summary>
    /// Key ID
    /// </summary>
    [JsonPropertyName("kid")]
    public string? Kid { get; init; }

    /// <summary>
    /// Algorithm
    /// </summary>
    [JsonPropertyName("alg")]
    public string? Alg { get; init; }

    /// <summary>
    /// Modulus
    /// </summary>
    [JsonPropertyName("n")]
    public string? N { get; init; }

    /// <summary>
    /// Exponent
    /// </summary>
    [JsonPropertyName("e")]
    public string? E { get; init; }

    /// <summary>
    /// Private exponent
    /// </summary>
    [JsonPropertyName("d")]
    public string? D { get; init; }

    /// <summary>
    /// First prime factor
    /// </summary>
    [JsonPropertyName("p")]
    public string? P { get; init; }

    /// <summary>
    /// Second prime factor
    /// </summary>
    [JsonPropertyName("q")]
    public string? Q { get; init; }

    /// <summary>
    /// First CRT coefficient
    /// </summary>
    [JsonPropertyName("qi")]
    public string? Qi { get; init; }

    /// <summary>
    /// First factor CRT exponent
    /// </summary>
    [JsonPropertyName("dp")]
    public string? Dp { get; init; }

    /// <summary>
    /// Second factor CRT exponent
    /// </summary>
    [JsonPropertyName("dq")]
    public string? Dq { get; init; }

    /// <summary>
    /// Validates the contents of this JWK
    /// </summary>
    public ValidationResult Validate()
    {
        var props = new Dictionary<string, string?>
        {
            [nameof(Kty)] = Kty,
            [nameof(Use)] = Use,
            [nameof(Kid)] = Kid,
            [nameof(Alg)] = Alg,
            [nameof(N)] = N,
            [nameof(E)] = E,
            [nameof(D)] = D,
            [nameof(P)] = P,
            [nameof(Q)] = Q,
            [nameof(Qi)] = Qi,
            [nameof(Dp)] = Dp,
            [nameof(Dq)] = Dq
        };

        return new ValidationResult
        {
            InvalidProperties = props.Where(x => string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).ToList()
        };
    }

    /// <summary>
    /// A <see cref="JsonWebKey"/> instance containing the component data from this record
    /// </summary>
    public JsonWebKey ToJsonWebKey()
    {
        return new JsonWebKey
        {
            Kty = Kty,
            Use = Use,
            Kid = Kid,
            Alg = Alg,
            N = N,
            E = E,
            D = D,
            P = P,
            Q = Q,
            QI = Qi,
            DP = Dp,
            DQ = Dq
        };
    }

    internal readonly record struct ValidationResult
    {
        public IEnumerable<string>? InvalidProperties { get; init; }

        public readonly bool IsValid() => InvalidProperties.IsNullOrEmpty();

        public override string ToString()
        {
            return IsValid()
                ? "All properties are valid"
                : $"The following required properties are empty: {string.Join(", ", InvalidProperties ?? [])}";
        }
    }
}
