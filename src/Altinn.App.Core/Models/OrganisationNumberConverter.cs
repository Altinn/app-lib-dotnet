using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models;

/// <summary>
/// Json converter to transform between <see cref="string"/> &amp; <see cref="OrganisationNumber"/>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
public class OrganisationNumberJsonConverterAttribute : JsonConverterAttribute
{
    /// <summary>
    /// The format to use for <see cref="OrganisationNumberJsonConverter.Write"/> operations
    /// </summary>
    public OrganisationNumberFormat Format { get; }

    /// <summary>
    /// Heya
    /// </summary>
    /// <param name="format"></param>
    public OrganisationNumberJsonConverterAttribute(OrganisationNumberFormat format)
    {
        Format = format;
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        return new OrganisationNumberJsonConverter(Format);
    }
}

internal class OrganisationNumberJsonConverter(OrganisationNumberFormat format) : JsonConverter<OrganisationNumber>
{
    public override OrganisationNumber Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string token for OrganisationNumber property.");
        }

        var numberValue = reader.GetString() ?? throw new JsonException("OrganisationNumber string value is null.");
        return OrganisationNumber.Parse(numberValue);
    }

    public override void Write(Utf8JsonWriter writer, OrganisationNumber value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Get(format));
    }
}
