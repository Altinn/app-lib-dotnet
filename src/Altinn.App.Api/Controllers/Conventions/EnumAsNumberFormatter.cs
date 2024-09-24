using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Altinn.App.Api.Controllers.Conventions;

internal class EnumAsNumberFormatter : SystemTextJsonOutputFormatter
{
    internal EnumAsNumberFormatter(string settingsName, JsonOptions options)
        : base(CreateSerializerOptions(options))
    {
        SettingsName = settingsName;
    }

    internal string SettingsName { get; }

    private static JsonSerializerOptions CreateSerializerOptions(JsonOptions options)
    {
        var newOptions = new JsonSerializerOptions(options.JsonSerializerOptions);
        newOptions.Converters.Add(new EnumToNumberJsonConverterFactory());
        return newOptions;
    }
}

internal class EnumToNumberJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var instance =
            Activator.CreateInstance(typeof(EnumToNumberJsonConverter<>).MakeGenericType(typeToConvert))
            ?? throw new InvalidOperationException($"Failed to create converter for type {typeToConvert.FullName}");
        return (JsonConverter)instance;
    }
}

internal class EnumToNumberJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return (TEnum)Enum.ToObject(typeToConvert, value);
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));
    }
}
