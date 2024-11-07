using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models;

/// <summary>
/// Json converter to transform between <see cref="string"/> &amp; <see cref="AccessToken"/>
/// </summary>
public class AccessTokenJsonConverter : JsonConverter<AccessToken>
{
    /// <inheritdoc/>
    public override AccessToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string token for AccessToken.");
        }

        var tokenValue = reader.GetString();
        return tokenValue is null
            ? throw new JsonException("AccessToken string value is null.")
            : AccessToken.Parse(tokenValue);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, AccessToken value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
