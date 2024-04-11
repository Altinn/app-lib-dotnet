using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InconsistentNaming
#pragma warning disable SA1300 // Inconsistent casing on property

namespace Altinn.App.Core.Tests.Helpers;

public class ObjectUtils_XmlSerializationTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private readonly Mock<ILogger> _loggerMock = new();

    [XmlRoot(ElementName = "model")]
    public class YttersteObjekt
    {
        [XmlElement("aarets", Order = 1)]
        [JsonPropertyName("aarets")]
        public DesimalMedORID? aarets { get; set; }

        [XmlElement("aarets2", Order = 2)]
        [JsonPropertyName("aarets2")]
        public StringMedORID? aarets2 { get; set; }

        [XmlElement("aarets3", Order = 3)]
        [JsonPropertyName("aarets3")]
        public string? aarets3 { get; set; }

        [XmlElement("children", Order = 4)]
        public List<YttersteObjekt>? children { get; set; }
    }

    public class DesimalMedORID
    {
        [XmlIgnore]
        public decimal? value { get; set; }

        [XmlText]
        [JsonIgnore]
        public string? valueAsString
        {
            get => value?.ToString();
            set
            {
                this.value = value == null ? null : (decimal?)decimal.Parse(value);
            }
        }

        [XmlAttribute("orid")]
        [BindNever]
        public string orid => "30320";
    }

    public class StringMedORID
    {
        [XmlText()]
        public string? value { get; set; }

        [XmlAttribute("orid")]
        [BindNever]
        public string orid => "30321";
    }

    public static TheoryData<string?, string?> StringTests => new()
    {
        // originalValue, storedValue
        { null,  null },
        { "some", "some" },
        { string.Empty, null },
        { " ", null },
        { "  ", null },
        { "  a", "  a" },
        { "  a ", "  a " },
        { "a  ", "a  " },
        { "a", "a" },
        { "a.", "a." },
        { "a.📚", "a.📚" },
    };

    [Theory]
    [MemberData(nameof(StringTests))]
    public void TestPrepareForStorage(string? value, string? storedValue)
    {
        var test = CreateObject(value);

        ObjectUtils.PrepareModelForXmlStorage(test);

        // prepareForXmlStorage should set all empty strings to null
        // but serialization only sets [XmlText] strings to null
        AssertObject(test, value, storedValue);
    }

    [Theory]
    [MemberData(nameof(StringTests))]
    public async Task TestSerializeDeserializeAsStorage(string? value, string? storedValue)
    {
        var test = CreateObject(value);

        // Serialize and deserialize twice to ensure that all changes in serialization is applied
        var testResult = await SerializeDeserialize(test);
        testResult = await SerializeDeserialize(testResult);

        if (storedValue is null)
        {
            // XmlSerialization does not set the parent object aarets2 to null, so we do that manually for all test cases to work
            testResult.aarets2.Should().NotBeNull();
            testResult.aarets2!.value.Should().BeNull();
            testResult.aarets2 = null;
            var child = testResult.children.Should().ContainSingle().Which;
            child.aarets2.Should().NotBeNull();
            child.aarets2!.value.Should().BeNull();
            child.aarets2 = null;
        }

        AssertObject(testResult, value, storedValue);
    }

    private async Task<YttersteObjekt> SerializeDeserialize(YttersteObjekt test)
    {
        // Serialize
        using var serializationStream = new MemoryStream();
        DataClient.Serialize<YttersteObjekt>(test, typeof(YttersteObjekt), serializationStream);

        serializationStream.Seek(0, SeekOrigin.Begin);
        _output.WriteLine(System.Text.Encoding.UTF8.GetString(serializationStream.ToArray()));

        // Deserialize
        ModelDeserializer serializer = new ModelDeserializer(_loggerMock.Object, typeof(YttersteObjekt));
        var deserialized = await serializer.DeserializeAsync(serializationStream, "application/xml");
        var testResult = deserialized.Should().BeOfType<YttersteObjekt>().Which;

        return testResult;
    }

    [Theory]
    [MemberData(nameof(StringTests))]
    public void TestSerializeDeserializeAsJson(string? value, string? storedValue)
    {
        _output.WriteLine($"Direct Json Serialization does not change {value} into {storedValue}");

        var test = CreateObject(value);

        // Serialize
        var json = JsonSerializer.Serialize(test);
        _output.WriteLine(json);

        // Deserialize
        var testResult = JsonSerializer.Deserialize<YttersteObjekt>(json)!;

        if (value is null)
        {
            // JsonSerialization does not set the parent object aarets2 to null, so we do that manually for all test cases to work
            testResult.aarets2.Should().NotBeNull();
            testResult.aarets2!.value.Should().BeNull();
            testResult.aarets2 = null;
            var child = testResult.children.Should().ContainSingle().Which;
            child.aarets2.Should().NotBeNull();
            child.aarets2!.value.Should().BeNull();
            child.aarets2 = null;
        }

        AssertObject(testResult, value, value);
    }

    private static YttersteObjekt CreateObject(string? value)
    {
        var test = new YttersteObjekt
        {
            aarets2 = new StringMedORID
            {
                value = value
            },
            aarets3 = value,
            children = new List<YttersteObjekt>
            {
                new YttersteObjekt
                {
                    aarets2 = new StringMedORID
                    {
                        value = value
                    },
                    aarets3 = value,
                }
            }
        };

        test.aarets.Should().BeNull();
        test.aarets2.Should().NotBeNull();
        test.aarets2!.value.Should().Be(value);
        test.aarets2.orid.Should().Be("30321");
        var child = test.children.Should().ContainSingle().Which;
        child.aarets.Should().BeNull();
        child.aarets2.Should().NotBeNull();
        child.aarets2!.value.Should().Be(value);
        child.aarets2.orid.Should().Be("30321");
        child.aarets3.Should().Be(value);
        return test;
    }

    private static void AssertObject(YttersteObjekt test, string? normalValue, string? xmlTextValue)
    {
        test.aarets.Should().BeNull();
        if (xmlTextValue is null)
        {
            test.aarets2.Should().BeNull();
        }
        else
        {
            test.aarets2.Should().NotBeNull();
            test.aarets2!.value.Should().Be(xmlTextValue);
            test.aarets2.orid.Should().Be("30321");
        }

        test.aarets3.Should().Be(normalValue);
        var child = test.children.Should().ContainSingle().Which;
        child.aarets.Should().BeNull();
        if (xmlTextValue is null)
        {
            child.aarets2.Should().BeNull();
        }
        else
        {
            child.aarets2.Should().NotBeNull();
            child.aarets2!.value.Should().Be(xmlTextValue);
            child.aarets2.orid.Should().Be("30321");
        }

        child.aarets3.Should().Be(normalValue);
    }
}
