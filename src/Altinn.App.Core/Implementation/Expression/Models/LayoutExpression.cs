using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Implementation.Expression;

[JsonConverter(typeof(LayoutExpressionConverter))]
public class LayoutExpression
{
    public string? Function { get; set; }
    public List<LayoutExpression>? Args { get; set; }
    public object? Value { get; set; }
}


/// <summary>
/// JsonConverter to be able to parse any valid LayoutExpression in Json format to the C# <see cref="LayoutExpression"/>
/// </summary>
/// <remarks>
/// This can possibly be extended to support shorthand 
/// </remarks>
public class LayoutExpressionConverter : JsonConverter<LayoutExpression>
{
    /// <inheritdoc />
    public override LayoutExpression? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadNotNull(ref reader, options);
    }

    /// 
    private LayoutExpression ReadNotNull(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => new LayoutExpression { Value = true },
            JsonTokenType.False => new LayoutExpression { Value = false },
            JsonTokenType.String => new LayoutExpression { Value = reader.GetString() },
            JsonTokenType.Number => new LayoutExpression { Value = reader.GetDouble() },
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.Null => new LayoutExpression { Value = null },
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            _ => throw new JsonException(),
        };
    }
    private LayoutExpression ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("First list item in a layout Expression must be a string");
        }
        var expr = new LayoutExpression(){
            Function = reader.GetString()!,
            Args = new List<LayoutExpression>()
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            expr.Args.Add(ReadNotNull(ref reader, options));
        }

        return expr;
    }


    private LayoutExpression ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var expr = new LayoutExpression();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName.Equals("function", StringComparison.InvariantCultureIgnoreCase))
            {
                expr.Function = reader.GetString();
            }
            else if (propertyName.Equals("args", StringComparison.InvariantCultureIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("Args in layout expression must be an array");
                }

                expr.Args = new List<LayoutExpression>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    expr.Args.Add(ReadNotNull(ref reader, options));
                }
            }
            else
            {
                throw new JsonException($"Unknown property \"{propertyName}\" in LayoutExpression. (Accepted: function, args)");
            }
        }

        if (expr.Function is null || expr.Args is null)
        {
            throw new JsonException($"LayoutExpression is missing required property function or args"); //TODO: Improve error mesage. This is likely to be hit for invalid json
        }

        return expr;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LayoutExpression value, JsonSerializerOptions options)
    {
        if (value.Function != null && value.Args != null)
        {
            // Serialize with a wrapper object {"function": ..., "args": ...}
            writer.WriteStartObject();
            writer.WriteString("function", value.Function);
            writer.WritePropertyName("args");
            JsonSerializer.Serialize(writer, value.Args, options);
            writer.WriteEndObject();
        }
        else
        {
            // Just serialize the literal value
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}