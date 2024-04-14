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

public class ObjectUtils_XmlSerializationTests(ITestOutputHelper _output)
{
    private readonly Mock<ILogger> _loggerMock = new();

    [XmlRoot(ElementName = "model")]
    public class YttersteObjekt
    {
        [XmlElement("aarets", Order = 1)]
        [JsonPropertyName("aarets")]
        public NullableDecimalMedORID? DecimalMedOrid { get; set; }

        public bool ShouldSerializeDecimalMedOrid()
        {
            return DecimalMedOrid?.value != null;
        }

        [XmlElement("aarets2", Order = 2)]
        [JsonPropertyName("aarets2")]
        public StringMedORID? StringMedOrid { get; set; }

        public bool ShouldSerializeStringMedOrid()
        {
            return StringMedOrid?.value != null;
        }

        [XmlElement("aarets3", Order = 3)]
        [JsonPropertyName("aarets3")]
        public string? NormalString { get; set; }

        public bool ShouldSerializeNormalString()
        {
            return NormalString != "should not serialize";
        }

        [XmlElement("aarets4", Order = 4)]
        [JsonPropertyName("aarets4")]
        public decimal? NullableDecimal { get; set; }

        public bool ShouldSerializeNullableDecimal()
        {
            return NullableDecimal != null && NullableDecimal != 1234567890;
        }

        [XmlElement("aarets5", Order = 5)]
        [JsonPropertyName("aarets5")]
        public decimal Decimal { get; set; }

        public bool ShouldSerializeDecimal()
        {
            return Decimal != 1234567890;
        }

        [XmlElement("children", Order = 6)]
        public List<YttersteObjekt>? Children { get; set; }
    }

    public class NullableDecimalMedORID
    {
        [XmlIgnore]
        [JsonPropertyName("value")]
        public decimal? value { get; set; }

        [XmlText]
        [JsonIgnore]
        public decimal valueOrDefault
        {
            get => value ?? default;
            set
            {
                this.value = value;
            }
        }

        [XmlAttribute("orid")]
        [JsonPropertyName("orid")]
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

    public static TheoryData<decimal?> DecimalTests => new()
    {
        { 123 },
        { 123.456m },
        { null },
    };

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
            testResult.StringMedOrid.Should().NotBeNull();
            testResult.StringMedOrid!.value.Should().BeNull();
            testResult.StringMedOrid = null;
            var child = testResult.Children.Should().ContainSingle().Which;
            child.StringMedOrid.Should().NotBeNull();
            child.StringMedOrid!.value.Should().BeNull();
            child.StringMedOrid = null;
        }

        AssertObject(testResult, value, value);
    }

    private static YttersteObjekt CreateObject(string? value)
    {
        var test = new YttersteObjekt
        {
            StringMedOrid = new StringMedORID
            {
                value = value
            },
            NormalString = value,
            Children = new List<YttersteObjekt>
            {
                new YttersteObjekt
                {
                    StringMedOrid = new StringMedORID
                    {
                        value = value
                    },
                    NormalString = value,
                }
            }
        };

        test.DecimalMedOrid.Should().BeNull();
        test.StringMedOrid.Should().NotBeNull();
        test.StringMedOrid!.value.Should().Be(value);
        test.StringMedOrid.orid.Should().Be("30321");
        var child = test.Children.Should().ContainSingle().Which;
        child.DecimalMedOrid.Should().BeNull();
        child.StringMedOrid.Should().NotBeNull();
        child.StringMedOrid!.value.Should().Be(value);
        child.StringMedOrid.orid.Should().Be("30321");
        child.NormalString.Should().Be(value);
        return test;
    }

    private static void AssertObject(YttersteObjekt test, string? normalValue, string? xmlTextValue)
    {
        test.DecimalMedOrid.Should().BeNull();
        if (xmlTextValue is null)
        {
            test.StringMedOrid.Should().BeNull();
        }
        else
        {
            test.StringMedOrid.Should().NotBeNull();
            test.StringMedOrid!.value.Should().Be(xmlTextValue);
            test.StringMedOrid.orid.Should().Be("30321");
        }

        test.NormalString.Should().Be(normalValue);
        var child = test.Children.Should().ContainSingle().Which;
        child.DecimalMedOrid.Should().BeNull();
        if (xmlTextValue is null)
        {
            child.StringMedOrid.Should().BeNull();
        }
        else
        {
            child.StringMedOrid.Should().NotBeNull();
            child.StringMedOrid!.value.Should().Be(xmlTextValue);
            child.StringMedOrid.orid.Should().Be("30321");
        }

        child.NormalString.Should().Be(normalValue);
    }

    [Theory]
    [MemberData(nameof(DecimalTests))]
    public void TestPrepareForStorage_Decimal(decimal? value)
    {
        var test = CreateObject(value);

        ObjectUtils.PrepareModelForXmlStorage(test);

        // prepareForXmlStorage should set all empty strings to null
        // but serialization only sets [XmlText] strings to null
        AssertObject(test, value);
    }

    [Theory]
    [MemberData(nameof(DecimalTests))]
    public async Task TestSerializeDeserializeAsStorage_Decimal(decimal? value)
    {
        var test = CreateObject(value);

        // Serialize and deserialize twice to ensure that all changes in serialization is applied
        var testResult = await SerializeDeserialize(test);
        testResult = await SerializeDeserialize(testResult);

        AssertObject(testResult, value);
    }

    [Theory]
    [MemberData(nameof(DecimalTests))]
    public void TestSerializeDeserializeAsJson_Decimal(decimal? value)
    {
        var test = CreateObject(value);

        // Serialize
        var json = JsonSerializer.Serialize(test);
        _output.WriteLine(json);

        // Deserialize
        var testResult = JsonSerializer.Deserialize<YttersteObjekt>(json)!;

        if (value is null)
        {
            // JsonSerialization does not set the parent object StringMedOrid to null, so we do that manually for all test cases to work
            testResult.DecimalMedOrid.Should().NotBeNull();
            testResult.DecimalMedOrid!.value.Should().BeNull();
            testResult.DecimalMedOrid = null;
            var child = testResult.Children.Should().ContainSingle().Which;
            child.DecimalMedOrid.Should().NotBeNull();
            child.DecimalMedOrid!.value.Should().BeNull();
            child.DecimalMedOrid = null;
        }

        AssertObject(testResult, value);
    }

    private static YttersteObjekt CreateObject(decimal? value)
    {
        var test = new YttersteObjekt
        {
            DecimalMedOrid = new NullableDecimalMedORID
            {
                value = value
            },
            NullableDecimal = value,
            Decimal = value ?? default,
            Children = new List<YttersteObjekt>
            {
                new YttersteObjekt
                {
                    DecimalMedOrid = new NullableDecimalMedORID
                    {
                        value = value
                    },
                    NullableDecimal = value,
                    Decimal = value ?? default,
                }
            }
        };

        test.StringMedOrid.Should().BeNull();
        test.DecimalMedOrid.Should().NotBeNull();
        test.DecimalMedOrid!.value.Should().Be(value);
        test.DecimalMedOrid.orid.Should().Be("30320");
        test.Decimal.Should().Be(value ?? default);
        test.NullableDecimal.Should().Be(value);
        var child = test.Children.Should().ContainSingle().Which;
        child.StringMedOrid.Should().BeNull();
        child.DecimalMedOrid.Should().NotBeNull();
        child.DecimalMedOrid!.value.Should().Be(value);
        child.DecimalMedOrid.orid.Should().Be("30320");
        child.Decimal.Should().Be(value ?? default);
        child.NullableDecimal.Should().Be(value);
        return test;
    }

    private static void AssertObject(YttersteObjekt test, decimal? value)
    {
        test.StringMedOrid.Should().BeNull();
        if (value is null)
        {
            test.DecimalMedOrid.Should().BeNull();
        }
        else
        {
            test.DecimalMedOrid.Should().NotBeNull();
            test.DecimalMedOrid!.value.Should().Be(value);
            test.DecimalMedOrid.orid.Should().Be("30320");
        }

        test.Decimal.Should().Be(value ?? default);
        test.NullableDecimal.Should().Be(value);
        var child = test.Children.Should().ContainSingle().Which;
        child.StringMedOrid.Should().BeNull();
        if (value is null)
        {
            child.DecimalMedOrid.Should().BeNull();
        }
        else
        {
            child.DecimalMedOrid.Should().NotBeNull();
            child.DecimalMedOrid!.value.Should().Be(value);
            child.DecimalMedOrid.orid.Should().Be("30320");
        }

        child.Decimal.Should().Be(value ?? default);
        child.NullableDecimal.Should().Be(value);
    }

    [Fact]
    public void VerifyShouldSerialize()
    {
        var test = new YttersteObjekt
        {
            DecimalMedOrid = new(),
            StringMedOrid = new(),
            NormalString = "should not serialize",
            NullableDecimal = 1234567890,
            Decimal = 1234567890,
        };

        test.DecimalMedOrid.Should().NotBeNull();
        test.StringMedOrid.Should().NotBeNull();
        test.NormalString.Should().NotBeNull();
        test.NullableDecimal.Should().NotBeNull();
        test.Decimal.Should().NotBe(0);

        ObjectUtils.PrepareModelForXmlStorage(test);

        test.DecimalMedOrid.Should().BeNull();
        test.StringMedOrid.Should().BeNull();
        test.NormalString.Should().BeNull();
        test.NullableDecimal.Should().BeNull();
        test.Decimal.Should().Be(default);
    }
}