using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Layout.Components;

namespace Altinn.App.Core.Internal.AppValidation;
[JsonConverter(typeof(LayoutErrorFinder))]
public class LayoutErrorFinder : JsonConverter<LayoutErrorFinder>
{
    private static readonly AsyncLocal<BaseComponent?> _component = new();
    /// <summary>
    /// JsonConverter does not support additonal arguments, so we need to hack it together with AsyncLocal
    /// </summary>
    public static void SetComponent(BaseComponent component)
    {
        _component.Value = component;
    }
    /// <inheritdoc />
    public override LayoutErrorFinder? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName == "data")
            {
                ReadData(ref reader, options);
            }
            else
            {
                // Ignore other properties than "data"
                reader.Skip();
            }
        }
        return null;
    }

    private void ReadData(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName.Equals("layout", StringComparison.InvariantCultureIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    ReadComponent(ref reader, options);
                }
            }
            else
            {
                reader.Skip();
            }
        }
    }

    private void ReadComponent(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        string? id = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); // Not possiblie?
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName.Equals("id", StringComparison.InvariantCultureIgnoreCase))
            {
                id = reader.GetString();
                if (id == _component.Value?.Id)
                {
                    throw new JsonException(); // Signal component location with an exception for the framework to add linenumber and byteinLine
                }
            }
            else
            {
                reader.Skip();
            }
        }

    }





    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LayoutErrorFinder value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
