using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Expression;

/// <summary>
/// JsonConverter to be able to parse any valid LayoutExpression in Json format to the C# <see cref="LayoutExpression"/>
/// </summary>
/// <remarks>
/// Currently this parser supports {"function":"funcname", "args": [arg1, arg2]} and ["funcname", arg1, arg2] syntax, and literal primitive types
/// </remarks>
public class LayoutExpressionConverter : JsonConverter<LayoutExpression>
{
    /// <inheritdoc />
    public override LayoutExpression? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadNotNull(ref reader, options);
    }

    /// <summary>
    /// Same as <see cref="Read" />, but without the nullable return type required by the interface. Throw an exeption instead.
    /// </summary>
    private LayoutExpression ReadNotNull(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => new LayoutExpression { Value = true },
            JsonTokenType.False => new LayoutExpression { Value = false },
            JsonTokenType.String => new LayoutExpression { Value = reader.GetString() },
            JsonTokenType.Number => new LayoutExpression { Value = reader.GetDouble() },
            JsonTokenType.Null => new LayoutExpression { Value = null },
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            JsonTokenType.StartObject => throw new JsonException("Invalid type \"object\""),
            _ => throw new JsonException(),
        };
    }

    private LayoutExpression ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            throw new JsonException("Missing function name in expression");
        }
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Function name in expression should be string");
        }
        var stringFunction = reader.GetString();
        if (!Enum.TryParse<LayoutExpressionFunctionEnum>(stringFunction, ignoreCase: false, out var functionEnum))
        {
            throw new JsonException($"Function \"{stringFunction}\" not implemented");
        }
        var expr = new LayoutExpression()
        {
            Function = functionEnum,
            Args = new List<LayoutExpression>()
        };

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            expr.Args.Add(ReadNotNull(ref reader, options));
        }

        return expr;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LayoutExpression value, JsonSerializerOptions options)
    {
        if (value.Function != null && value.Args != null)
        {
            // Serialize with as an array expression ["functionName", arg1, arg2, ...]
            writer.WriteStartArray();
            writer.WriteStringValue(value.Function.ToString());
            foreach (var arg in value.Args)
            {
                JsonSerializer.Serialize(writer, arg, options);
            }
            writer.WriteEndArray();
        }
        else
        {
            // Just serialize the literal value
            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}