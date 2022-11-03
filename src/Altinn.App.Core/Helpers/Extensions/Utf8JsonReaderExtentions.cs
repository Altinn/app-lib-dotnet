using System.Text.Json;
namespace Altinn.App.Core.Helpers.Extensions;

internal static class Utf8JsonReaderExtensions
{
    private static readonly JsonWriterOptions OPTIONS = new()
    {
        Indented = true,
    };

    internal static string SkipReturnString(this ref Utf8JsonReader reader)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, OPTIONS);
        Copy(ref reader, writer);
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Copy(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.None:
                writer.WriteNullValue();
                break;
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                while(reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    writer.WritePropertyName(reader.ValueSpan);
                    reader.Read();
                    Copy(ref reader, writer);
                }
                writer.WriteEndObject();
                return;
            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    Copy(ref reader, writer);
                }
                writer.WriteEndArray();
                return;
            case JsonTokenType.PropertyName:
                throw new JsonException("something is wrong, did not expect property name here");
            case JsonTokenType.Comment:
                writer.WriteCommentValue(reader.ValueSpan);
                break;
            case JsonTokenType.String:
                writer.WriteStringValue(reader.ValueSpan);
                break;
            case JsonTokenType.Number:
                writer.WriteNumberValue(reader.GetDouble());
                break;
            case JsonTokenType.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonTokenType.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonTokenType.Null:
                writer.WriteNullValue();
                break;
        }
    }
}