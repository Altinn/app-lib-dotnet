using System.Globalization;
using System.Text.Json.Serialization;
using Altinn.App.Core.Helpers;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Helpers;

public class ObjectUtilsTests
{
    public class TestClass
    {
        public Guid AltinnRowId { get; set; }

        public string? StringValue { get; set; }

        public decimal Decimal { get; set; }

        public decimal? NullableDecimal { get; set; }

        [JsonIgnore]
        public decimal DecimalIgnore { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public decimal? NullableDecimalIgnore { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal DecimalNotReallyIgnore { get; set; }

        public DateTime? DateTime { get; set; }

        public TestClass? Child { get; set; }

        public List<TestClass>? Children { get; set; }

        public long Long { get; set; }

        public long? NullableLong { get; set; }

        [JsonIgnore]
        public long LongIgnore { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long LongNotReallyIgnore { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public long? NullableLongIgnore { get; set; }
    }

    [Fact]
    public void TestSimple()
    {
        var test = new TestClass();
        test.Children.Should().BeNull();

        ObjectUtils.InitializeAltinnRowId(test);
        ObjectUtils.PrepareModelForXmlStorage(test);

        test.Children.Should().BeEmpty();
    }

    [Fact]
    public void TestSimpleStringInitialized()
    {
        var test = new TestClass() { StringValue = "some", };
        test.Children.Should().BeNull();

        ObjectUtils.InitializeAltinnRowId(test);
        ObjectUtils.PrepareModelForXmlStorage(test);

        test.Children.Should().BeEmpty();
        test.StringValue.Should().Be("some");
    }

    [Fact]
    public void TestSimpleListInitialized()
    {
        var test = new TestClass() { Children = new(), };
        test.Children.Should().BeEmpty();

        ObjectUtils.InitializeAltinnRowId(test);

        test.Children.Should().BeEmpty();
    }

    [Fact]
    public void TestMultipleLevelsInitialized()
    {
        var test = new TestClass()
        {
            Child = new TestClass()
            {
                Child = new TestClass()
                {
                    Child = new TestClass() { Children = new() { new TestClass() { Child = new TestClass() } } }
                }
            }
        };
        test.Children.Should().BeNull();
        test.Child.Children.Should().BeNull();
        test.Child.Child.Children.Should().BeNull();
        var subChild = test.Child.Child.Child.Children.Should().ContainSingle().Which;
        subChild.Children.Should().BeNull();
        subChild.Child.Should().NotBeNull();
        subChild.Child!.Children.Should().BeNull();

        // Act
        ObjectUtils.InitializeAltinnRowId(test);
        ObjectUtils.PrepareModelForXmlStorage(test);

        // Assert
        test.Children.Should().BeEmpty();
        test.Child.Children.Should().BeEmpty();
        test.Child.Child.Children.Should().BeEmpty();
        subChild = test.Child.Child.Child.Children.Should().ContainSingle().Which;
        subChild.Children.Should().BeEmpty();
        subChild.Child.Should().NotBeNull();
        subChild.Child!.Children.Should().BeEmpty();
    }

    [Fact]
    public void TestGuidInitialized()
    {
        var dateTime = DateTime.Parse("2021-01-01");
        var test = new TestClass()
        {
            Child = new(),
            Children = new List<TestClass>() { new TestClass(), new TestClass() },
            DateTime = dateTime,
            NullableDecimal = 1.1m,
            Decimal = 2.2m,
        };
        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Children.Should().AllSatisfy(c => c.AltinnRowId.Should().Be(Guid.Empty));

        ObjectUtils.InitializeAltinnRowId(test);

        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Children.Should().AllSatisfy(c => c.AltinnRowId.Should().NotBe(Guid.Empty));
        test.DateTime.Should().Be(dateTime);
        test.NullableDecimal.Should().Be(1.1m);
        test.Decimal.Should().Be(2.2m);

        ObjectUtils.RemoveAltinnRowId(test);

        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Children.Should().AllSatisfy(c => c.AltinnRowId.Should().Be(Guid.Empty));
    }

    [Fact]
    public void TestRemoveAltinnRowId()
    {
        var test = new TestClass()
        {
            AltinnRowId = Guid.NewGuid(),
            Child = new()
            {
                AltinnRowId = Guid.NewGuid(),
                Child = new()
                {
                    AltinnRowId = Guid.NewGuid(),
                    Children = new()
                    {
                        new TestClass()
                        {
                            AltinnRowId = Guid.NewGuid(),
                            Child = new() { AltinnRowId = Guid.NewGuid() }
                        }
                    }
                }
            }
        };
        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.Child!.AltinnRowId.Should().NotBe(Guid.Empty);

        ObjectUtils.RemoveAltinnRowId(test);

        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.Child!.AltinnRowId.Should().Be(Guid.Empty);

        ObjectUtils.InitializeAltinnRowId(test);

        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.Children.Should().ContainSingle().Which.Child!.AltinnRowId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TestRemoveAltinnRowIdWithNulls()
    {
        var test = new TestClass()
        {
            AltinnRowId = Guid.NewGuid(),
            Child = new()
            {
                AltinnRowId = Guid.NewGuid(),
                Child = new()
                {
                    AltinnRowId = Guid.NewGuid(),
                    Children = new()
                    {
                        new TestClass()
                        {
                            AltinnRowId = Guid.NewGuid(),
                            Child = new() { AltinnRowId = Guid.NewGuid() }
                        },
                        null!,
                    }
                }
            }
        };
        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        var childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().NotBe(Guid.Empty);
        childArray.ContainSingle(d => d == null);

        ObjectUtils.RemoveAltinnRowId(test);

        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().Be(Guid.Empty);
        childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().Be(Guid.Empty);
        childArray.ContainSingle(d => d == null);

        ObjectUtils.InitializeAltinnRowId(test);

        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().NotBe(Guid.Empty);
        childArray.ContainSingle(d => d == null);
    }

    [Fact]
    public void TestInitializeRowIdWithNulls()
    {
        var test = new TestClass()
        {
            AltinnRowId = Guid.Empty,
            Child = new()
            {
                AltinnRowId = Guid.Empty,
                Child = new()
                {
                    AltinnRowId = Guid.Empty,
                    Children = new()
                    {
                        new TestClass()
                        {
                            AltinnRowId = Guid.Empty,
                            Child = new() { AltinnRowId = Guid.Empty }
                        },
                        null!,
                    }
                }
            }
        };
        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().Be(Guid.Empty);
        var childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().Be(Guid.Empty);
        childArray.ContainSingle(d => d == null);

        ObjectUtils.InitializeAltinnRowId(test);

        test.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().NotBe(Guid.Empty);
        childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().NotBe(Guid.Empty);
        childArray.ContainSingle(d => d == null);

        ObjectUtils.RemoveAltinnRowId(test);

        test.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.AltinnRowId.Should().Be(Guid.Empty);
        test.Child.Child.AltinnRowId.Should().Be(Guid.Empty);
        childArray = test.Child.Child.Children.Should().HaveCount(2).And;
        childArray.ContainSingle(d => d != null).Which.AltinnRowId.Should().Be(Guid.Empty);
        childArray.ContainSingle(d => d == null);
    }

    [Theory]
    [InlineData(1, 3, "0.333333333333333")]
    [InlineData(2, 3, "0.666666666666667")]
    [InlineData(1, 10, "0.1")]
    [InlineData(2, 10, "0.2")]
    [InlineData(3, 10, "0.3")]
    [InlineData(4, 10, "0.4")]
    [InlineData(5, 10, "0.5")]
    [InlineData(6, 10, "0.6")]
    [InlineData(7, 10, "0.7")]
    [InlineData(8, 10, "0.8")]
    [InlineData(9, 10, "0.9")]
    [InlineData(10, 10, "1")]
    [InlineData(22, 7, "3.14285714285714")]
    public void TestDecimalRounding(int numerator, int denominator, string expectedValueString)
    {
        // Decimal values in model must be rounded to 15 significant figures to ensure that json conversion is lossless
        var startValue = decimal.Divide(numerator, denominator);
        var expectedValue = decimal.Parse(expectedValueString, CultureInfo.InvariantCulture);

        var test = new TestClass()
        {
            Decimal = startValue,
            NullableDecimal = startValue,
            DecimalIgnore = startValue,
            NullableDecimalIgnore = startValue,
            DecimalNotReallyIgnore = startValue,
        };

        ObjectUtils.PrepareModelForXmlStorage(test);

        test.Decimal.Should().Be(expectedValue);
        test.NullableDecimal.Should().Be(expectedValue);
        test.DecimalNotReallyIgnore.Should().Be(expectedValue);

        // Decimal properties with [JsonIgnore] should not be rounded
        test.DecimalIgnore.Should().Be(startValue);
        test.NullableDecimalIgnore.Should().Be(startValue);
    }

    [Fact]
    public void TestLongRounding()
    {
        // Long values in model must be rounded to 15 significant figures to ensure that json conversion is lossless
        var value = 1234567890123456789L;
        var roundedValue = 1234567890123460000L;
        var test = new TestClass()
        {
            Long = value,
            NullableLong = value,
            LongIgnore = value,
            NullableLongIgnore = value,
            LongNotReallyIgnore = value,
        };

        ObjectUtils.PrepareModelForXmlStorage(test);

        test.Long.Should().Be(roundedValue);
        test.NullableLong.Should().Be(roundedValue);
        test.LongNotReallyIgnore.Should().Be(roundedValue);

        test.LongIgnore.Should().Be(value);
        test.NullableLongIgnore.Should().Be(value);
    }
}
