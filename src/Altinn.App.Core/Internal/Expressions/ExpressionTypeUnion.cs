using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.Expressions;

/// <summary>
/// Discriminated union for the JSON types that can be arguments and result of expressions
/// </summary>
[JsonConverter(typeof(ExpressionTypeUnionConverter))]
public readonly struct ExpressionTypeUnion
{
    private readonly JsonValueKind _valueKind;
    private readonly string? _stringValue = null;

    // double is a value type where nullable takes extra space, and we only read it when it should be set
    private readonly double _numberValue = 0;

    // private readonly Dictionary<string, ExpressionTypeUnion>? _objectValue = null;
    // private readonly ExpressionTypeUnion[]? _arrayValue = null;

    /// <summary>
    /// Constructor for NULL value
    /// </summary>
    public ExpressionTypeUnion()
    {
        _valueKind = JsonValueKind.Null;
    }

    private ExpressionTypeUnion(bool? value)
    {
        if (value.HasValue)
        {
            _valueKind = value.Value ? JsonValueKind.True : JsonValueKind.False;
        }
        else
        {
            _valueKind = JsonValueKind.Null;
        }
    }

    private ExpressionTypeUnion(double? value)
    {
        if (value.HasValue)
        {
            _valueKind = JsonValueKind.Number;
            _numberValue = value.Value;
        }
        else
        {
            _valueKind = JsonValueKind.Null;
        }
    }

    private ExpressionTypeUnion(string? value)
    {
        _valueKind = value is null ? JsonValueKind.Null : JsonValueKind.String;
        _stringValue = value;
    }

    // private ExpressionTypeUnion(Dictionary<string, ExpressionTypeUnion>? value)
    // {
    //     _valueKind = value is null ? JsonValueKind.Null : JsonValueKind.Object;
    //     _objectValue = value;
    // }

    // private ExpressionTypeUnion(ExpressionTypeUnion[]? value)
    // {
    //     _valueKind = value is null ? JsonValueKind.Null : JsonValueKind.Array;
    //     _arrayValue = value;
    // }

    /// <summary>
    /// Convert a nullable boolean to ExpressionTypeUnion
    /// </summary>
    public static implicit operator ExpressionTypeUnion(bool? value) => new(value);

    /// <summary>
    /// Convert a nullable double to ExpressionTypeUnion
    /// </summary>
    public static implicit operator ExpressionTypeUnion(double? value) => new(value);

    /// <summary>
    /// Convert a nullable string to ExpressionTypeUnion
    /// </summary>
    public static implicit operator ExpressionTypeUnion(string? value) => new(value);

    // /// <summary>
    // /// Convert a Dictionary to ExpressionTypeUnion
    // /// </summary>
    // public static implicit operator ExpressionTypeUnion(Dictionary<string, ExpressionTypeUnion>? value) => new(value);
    //
    // /// <summary>
    // /// Convert an array to ExpressionTypeUnion
    // /// </summary>
    // public static implicit operator ExpressionTypeUnion(ExpressionTypeUnion[]? value) => new(value);

    /// <summary>
    /// Convert any of the supported CLR types to an expressionTypeUnion
    /// </summary>
    public static ExpressionTypeUnion FromObject(object? value)
    {
        return (ExpressionTypeUnion)(
            value switch
            {
                ExpressionTypeUnion expressionValue => expressionValue,
                null => new ExpressionTypeUnion(),
                bool boolValue => boolValue,
                string stringValue => stringValue,
                float numberValue => (double?)numberValue,
                double numberValue => (double?)numberValue,
                byte numberValue => (double?)numberValue,
                sbyte numberValue => (double?)numberValue,
                short numberValue => (double?)numberValue,
                ushort numberValue => (double?)numberValue,
                int numberValue => (double?)numberValue,
                uint numberValue => (double?)numberValue,
                long numberValue => (double?)numberValue,
                ulong numberValue => (double?)numberValue,
                decimal numberValue => (double?)numberValue, // expressions uses double which needs an explicit cast

                DateTime dateTimeValue => JsonSerializer.Serialize(dateTimeValue),
                DateTimeOffset dateTimeOffsetValue => JsonSerializer.Serialize(dateTimeOffsetValue),
                TimeSpan timeSpanValue => JsonSerializer.Serialize(timeSpanValue),
                TimeOnly timeOnlyValue => JsonSerializer.Serialize(timeOnlyValue),
                DateOnly dateOnlyValue => JsonSerializer.Serialize(dateOnlyValue),

                // Dictionary<string, ExpressionTypeUnion> objectValue => new ExpressionTypeUnion(objectValue),
                // TODO add support for arrays, objects and other potential types
                _ => new ExpressionTypeUnion(),
            }
        );
    }

    /// <summary>
    /// Convert the value to the relevant CLR type
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public object? ToObject() =>
        ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => String,
            JsonValueKind.Number => Number,
            // JsonValueKind.Object => Object,
            // JsonValueKind.Array => Array,
            _ => throw new InvalidOperationException("Invalid value kind"),
        };

    /// <summary>
    /// Get the type of json value this represents
    /// </summary>
    public JsonValueKind ValueKind => _valueKind;

    /// <summary>
    /// Get the value as a boolean (or throw if it isn't a boolean ValueKind)
    /// </summary>
    public bool Bool =>
        _valueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidOperationException($"{Json} is a boolean"),
        };

    /// <summary>
    /// Get the value as a string (or throw if it isn't a string ValueKind)
    /// </summary>
    public string String =>
        _valueKind switch
        {
            JsonValueKind.String => _stringValue ?? throw new UnreachableException("Not a string"),
            _ => throw new InvalidOperationException($"{Json} is not a string"),
        };

    /// <summary>
    /// Get the value as a number (or throw if it isn't a number ValueKind)
    /// </summary>
    public double Number =>
        _valueKind switch
        {
            JsonValueKind.Number => _numberValue,
            _ => throw new InvalidOperationException($"{Json} is not a number"),
        };

    // public Dictionary<string, ExpressionTypeUnion> Object =>
    //     _valueKind switch
    //     {
    //         JsonValueKind.Object => _objectValue ?? throw new UnreachableException($"{Json} is not an object"),
    //         _ => throw new InvalidOperationException($"{Json} is not an object"),
    //     };
    //
    // public ExpressionTypeUnion[] Array =>
    //     _valueKind switch
    //     {
    //         JsonValueKind.Array => _arrayValue ?? throw new UnreachableException($"{Json} is not an array"),
    //         _ => throw new InvalidOperationException($"{Json} is not an array"),
    //     };

    /// <summary>
    /// Get the value as it would be serialized to JSON
    /// </summary>
    public string Json =>
        ValueKind switch
        {
            JsonValueKind.Null => "null",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.String => JsonSerializer.Serialize(String),
            JsonValueKind.Number => JsonSerializer.Serialize(Number),
            // JsonValueKind.Object => JsonSerializer.Serialize(Object),
            // JsonValueKind.Array => JsonSerializer.Serialize(Array),
            _ => throw new InvalidOperationException("Invalid value kind"),
        };
}

/// <summary>
/// JsonTypeUnion should serialize as the json value it represents, and the properties can't be accessed directly anyway
/// </summary>
internal class ExpressionTypeUnionConverter : JsonConverter<ExpressionTypeUnion>
{
    /// <inheritdoc />
    public override ExpressionTypeUnion Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.Read();
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.Null => new ExpressionTypeUnion(),
            // JsonTokenType.StartObject => ReadObject(ref reader),
            // JsonTokenType.StartArray => ReadArray(ref reader),
            _ => throw new JsonException(),
        };
    }

    // private ExpressionTypeUnion ReadArray(ref Utf8JsonReader reader)
    // {
    //     throw new NotImplementedException();
    // }
    //
    // private ExpressionTypeUnion ReadObject(ref Utf8JsonReader reader)
    // {
    //     throw new NotImplementedException();
    // }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ExpressionTypeUnion value, JsonSerializerOptions options)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(value.String);
                break;
            case JsonValueKind.Number:
                writer.WriteNumberValue(value.Number);
                break;
            // case JsonValueKind.Object:
            //     JsonSerializer.Serialize(writer, value.Object, options);
            //     break;
            // case JsonValueKind.Array:
            //     JsonSerializer.Serialize(writer, value.Array, options);
            //     break;
            default:
                throw new JsonException();
        }
        ;
    }
}
