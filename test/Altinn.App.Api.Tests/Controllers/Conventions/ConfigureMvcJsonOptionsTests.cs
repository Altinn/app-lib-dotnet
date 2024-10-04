using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Altinn.App.Api.Controllers.Conventions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Altinn.App.Api.Tests.Controllers.Conventions;

public class ConfigureMvcJsonOptionsTests
{
    [Fact]
    public void Configure_InsertsCustomFormatterWithCorrectSettings()
    {
        // Arrange
        var jsonSettingsName = "AltinnApi";
        var configureOptions = new ConfigureMvcJsonOptions(jsonSettingsName);
        var mvcOptions = new MvcOptions();

        // Create default JsonSerializerOptions with JsonStringEnumConverter
        var defaultSerializerOptions = new JsonSerializerOptions();
        defaultSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        defaultSerializerOptions.Encoder = JavaScriptEncoder.Default;
        defaultSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        // Add the default SystemTextJsonOutputFormatter
        var defaultJsonFormatter = new SystemTextJsonOutputFormatter(defaultSerializerOptions);
        mvcOptions.OutputFormatters.Add(defaultJsonFormatter);

        // Act
        configureOptions.Configure(mvcOptions);

        // Assert
        var customFormatter = mvcOptions.OutputFormatters.OfType<AltinnApiJsonFormatter>().FirstOrDefault();

        Assert.NotNull(customFormatter);
        Assert.Equal(jsonSettingsName, customFormatter.SettingsName);

        var indexOfDefaultFormatter = mvcOptions.OutputFormatters.IndexOf(defaultJsonFormatter);
        var indexOfCustomFormatter = mvcOptions.OutputFormatters.IndexOf(customFormatter);

        Assert.Equal(indexOfDefaultFormatter - 1, indexOfCustomFormatter);

        var customSerializerOptions = customFormatter.SerializerOptions;
        var hasEnumConverter = customSerializerOptions.Converters.Any(c => c is JsonStringEnumConverter);

        Assert.False(
            hasEnumConverter,
            "JsonStringEnumConverter should have been removed from the custom formatter's SerializerOptions"
        );

        Assert.NotNull(customSerializerOptions.Encoder);

        string testString = "<>&\"'©";
        string expectedEncodedString = JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(testString);
        string actualEncodedString = customSerializerOptions.Encoder.Encode(testString);

        Assert.Equal(expectedEncodedString, actualEncodedString);
    }
}
