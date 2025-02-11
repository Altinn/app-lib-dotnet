using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Expressions;

/// <summary>
/// JsonConverter to be able to parse any valid Expression in Json format to the C# <see cref="Expression"/>
/// </summary>
/// <remarks>
/// Currently this parser supports {"function":"funcname", "args": [arg1, arg2]} and ["funcname", arg1, arg2] syntax, and literal primitive types
/// </remarks>
public class ExpressionConverter : JsonConverter<Expression>
{
    /// <inheritdoc />
    public override Expression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadStatic(ref reader, options);
    }

    /// <summary>
    /// Same as <see cref="Read" />, but without the nullable return type required by the interface. Throw an exeption instead.
    /// </summary>
    public static Expression ReadStatic(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => new Expression(true),
            JsonTokenType.False => new Expression(false),
            JsonTokenType.String => new Expression(reader.GetString()),
            JsonTokenType.Number => new Expression(reader.GetDouble()),
            JsonTokenType.Null => new Expression(null),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            JsonTokenType.StartObject => throw new JsonException("Invalid type \"object\""),
            _ => throw new JsonException(),
        };
    }

    private static Expression ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
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

        var functionEnum = ParseFunctionName(ref reader);

        var args = new List<Expression>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            args.Add(ReadStatic(ref reader, options));
        }

        return new Expression(functionEnum, args);
    }

    private static ExpressionFunction ParseFunctionName(ref Utf8JsonReader reader)
    {
        var functionSpan = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        return functionSpan switch
        {
            [100, 97, 116, 97, 77, 111, 100, 101, 108] => ExpressionFunction.dataModel,
            [99, 111, 109, 112, 111, 110, 101, 110, 116] => ExpressionFunction.component,
            [105, 110, 115, 116, 97, 110, 99, 101, 67, 111, 110, 116, 101, 120, 116] =>
                ExpressionFunction.instanceContext,
            [105, 102] => ExpressionFunction.@if,
            [102, 114, 111, 110, 116, 101, 110, 100, 83, 101, 116, 116, 105, 110, 103, 115] =>
                ExpressionFunction.frontendSettings,
            [99, 111, 110, 99, 97, 116] => ExpressionFunction.concat,
            [117, 112, 112, 101, 114, 67, 97, 115, 101] => ExpressionFunction.upperCase,
            [108, 111, 119, 101, 114, 67, 97, 115, 101] => ExpressionFunction.lowerCase,
            [99, 111, 110, 116, 97, 105, 110, 115] => ExpressionFunction.contains,
            [110, 111, 116, 67, 111, 110, 116, 97, 105, 110, 115] => ExpressionFunction.notContains,
            [99, 111, 109, 109, 97, 67, 111, 110, 116, 97, 105, 110, 115] => ExpressionFunction.commaContains,
            [101, 110, 100, 115, 87, 105, 116, 104] => ExpressionFunction.endsWith,
            [115, 116, 97, 114, 116, 115, 87, 105, 116, 104] => ExpressionFunction.startsWith,
            [101, 113, 117, 97, 108, 115] => ExpressionFunction.equals,
            [110, 111, 116, 69, 113, 117, 97, 108, 115] => ExpressionFunction.notEquals,
            [103, 114, 101, 97, 116, 101, 114, 84, 104, 97, 110, 69, 113] => ExpressionFunction.greaterThanEq,
            [108, 101, 115, 115, 84, 104, 97, 110] => ExpressionFunction.lessThan,
            [108, 101, 115, 115, 84, 104, 97, 110, 69, 113] => ExpressionFunction.lessThanEq,
            [103, 114, 101, 97, 116, 101, 114, 84, 104, 97, 110] => ExpressionFunction.greaterThan,
            [115, 116, 114, 105, 110, 103, 76, 101, 110, 103, 116, 104] => ExpressionFunction.stringLength,
            [114, 111, 117, 110, 100] => ExpressionFunction.round,
            [97, 110, 100] => ExpressionFunction.and,
            [111, 114] => ExpressionFunction.or,
            [110, 111, 116] => ExpressionFunction.not,
            [118, 97, 108, 117, 101] => ExpressionFunction.value,
            [97, 114, 103, 118] => ExpressionFunction.value, // "argv" works for compatibility
            [103, 97, 116, 101, 119, 97, 121, 65, 99, 116, 105, 111, 110] => ExpressionFunction.gatewayAction,
            [108, 97, 110, 103, 117, 97, 103, 101] => ExpressionFunction.language,
            _ => throw new JsonException(
                $"Function \"{System.Text.Encoding.UTF8.GetString(functionSpan)}\" not implemented"
            ),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Expression value, JsonSerializerOptions options)
    {
        if (value.IsFunctionExpression)
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
