using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Maskinporten.Models;
using Microsoft.IdentityModel.Tokens;

namespace Altinn.App.Core.Features.Maskinporten.Converters;

/// <summary>
/// Reads a JSON blob containing a jwk, converting it to a <see cref="JsonWebKey"/> instance
/// </summary>
public class JsonWebKeyConverter : JsonConverter<JsonWebKey>
{
    /// <inheritdoc/>
    public override JsonWebKey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var jwk = new JwkWrapper();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected object, but found {reader.TokenType}");
            }

            string? propertyName = reader.GetString();
            reader.Read();

            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                continue;
            }

            string? propertyValue = reader.GetString();

            jwk = propertyName switch
            {
                "p" => jwk with { P = propertyValue },
                "kty" => jwk with { Kty = propertyValue },
                "q" => jwk with { Q = propertyValue },
                "d" => jwk with { D = propertyValue },
                "e" => jwk with { E = propertyValue },
                "use" => jwk with { Use = propertyValue },
                "kid" => jwk with { Kid = propertyValue },
                "qi" => jwk with { Qi = propertyValue },
                "dp" => jwk with { Dp = propertyValue },
                "alg" => jwk with { Alg = propertyValue },
                "dq" => jwk with { Dq = propertyValue },
                "n" => jwk with { N = propertyValue },
                _ => jwk
            };
        }

        var validationResult = jwk.Validate();

        return validationResult.IsValid()
            ? jwk.ToJsonWebKey()
            : throw new JsonException(
                $"The JsonWebKey is invalid after deserialization, not all required properties were found: {validationResult}"
            );
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, JsonWebKey value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
