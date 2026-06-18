using System.Text;
using System.Text.Json;

namespace Altinn.App.Core.Helpers.Extensions;

internal static class Utf8JsonReaderExtensions
{
    private static readonly JsonWriterOptions _options = new() { Indented = true };

    internal static string SkipReturnString(this ref Utf8JsonReader reader)
    {
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, _options);
        Copy(ref reader, writer);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static void WriteRawFormattedValue(this Utf8JsonWriter writer, string json)
    {
        var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), isFinalBlock: true, state: default);
        jsonReader.Read(); // Need to read first token to initialize the reader
        Copy(ref jsonReader, writer);
    }

    private static void Copy(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.None:
                throw new JsonException("Reader is not initialized");
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            writer.WritePropertyName(reader.ValueSpan);
                            reader.Read();
                            Copy(ref reader, writer);
                            break;
                        case JsonTokenType.Comment:
                            writer.WriteCommentValue(reader.ValueSpan);
                            break;
                        case JsonTokenType.EndObject:
                            writer.WriteEndObject();
                            return;
                        default:
                            throw new JsonException($"Something is wrong, did not expect {reader.TokenType} here");
                    }
                }
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException("Something is wrong, did not find end of object");
                }
                break;
            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    Copy(ref reader, writer);
                }
                writer.WriteEndArray();
                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    throw new JsonException("Something is wrong, did not find end of array");
                }
                break;
            case JsonTokenType.Comment:
                writer.WriteCommentValue(reader.ValueSpan);
                break;
            case JsonTokenType.String:
                writer.WriteStringValue(reader.ValueSpan);
                break;
            case JsonTokenType.Number:
                if (reader.HasValueSequence)
                {
                    writer.WriteRawValue(reader.ValueSequence);
                }
                else
                {
                    writer.WriteRawValue(reader.ValueSpan);
                }
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
            default:
                throw new JsonException($"Something is wrong, did not expect {reader.TokenType} here");
        }
    }
}
