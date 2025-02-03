using System.Numerics;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.LayoutExpressions.ExpressionEvaluatorTests;

public class EqualTests(ITestOutputHelper outputHelper)
{
    private static void AddIfEqual(TheoryData<object> data, object value, double origValue)
    {
        double newValue = Convert.ToDouble(value);
        if (origValue.Equals(newValue))
        {
            data.Add(value);
        }
    }

    public static TheoryData<object> GetNumericTestData(double value)
    {
        var data = new TheoryData<object>();
        AddIfEqual(data, (byte)value, value);
        AddIfEqual(data, (sbyte)value, value);
        AddIfEqual(data, (short)value, value);
        AddIfEqual(data, (ushort)value, value);
        AddIfEqual(data, (int)value, value);
        AddIfEqual(data, (uint)value, value);
        AddIfEqual(data, (long)value, value);
        AddIfEqual(data, (ulong)value, value);
        AddIfEqual(data, (float)value, value);
        AddIfEqual(data, (decimal)value, value);

        return data;
    }

    public static TheoryData<object> GetExoticTypes =>
        new()
        {
            "123",
            true,
            false,
            "",
            DateTime.Now,
            DateOnly.FromDateTime(DateTime.Now),
            TimeOnly.FromDateTime(DateTime.Now),
            ((long)int.MaxValue) + 1,
            ((ulong)uint.MaxValue) + 1,
            ((decimal)int.MaxValue) + 1,
            ((decimal)uint.MaxValue) + 1,
            (double)((decimal)long.MaxValue + 1),
            (double)((decimal)ulong.MaxValue + 1),
        };

    [Theory]
    [MemberData(nameof(GetNumericTestData), 123.0)]
    [MemberData(nameof(GetNumericTestData), 0.5)]
    [MemberData(nameof(GetNumericTestData), -123.0)]
    [MemberData(nameof(GetExoticTypes))]
    public void ToStringForEquals_AgreesWithJsonSerializer(object? value)
    {
        outputHelper.WriteLine($"Object of type {value?.GetType().FullName ?? "null"}:");
        outputHelper.WriteLine($"   value:{value}");
        outputHelper.WriteLine($"   json: {JsonSerializer.Serialize(value)}");

        var union = ExpressionTypeUnion.FromObject(value);
        outputHelper.WriteLine($"   union: {union}");
        // Verify that the EqualsToString method returns the same value as the JsonSerializer.
        var json = value is string ? value : JsonSerializer.Serialize(value);
        var toStringForEquals = ExpressionEvaluator.ToStringForEquals(ExpressionTypeUnion.FromObject(value));
        Assert.Equal(json, toStringForEquals);
    }

    public static TheoryData<object> GetNonsenseValues =>
        new()
        {
            new BigInteger(123), // Not supported by JsonSerializer, but might make sense to support
            new object[] { 1, 2, 3 },
            new object(),
            new
            {
                A = 1,
                B = 2,
                C = 3,
            },
            new byte[] { 0x01, 0x02, 0x03 },
        };

    [Theory]
    [MemberData(nameof(GetNonsenseValues))]
    public void ToStringForEquals_NonsenseTypes_ThrowsException(object? value)
    {
        outputHelper.WriteLine($"Object of type {value?.GetType().FullName ?? "null"}:");
        outputHelper.WriteLine($"   value:{value}");
        outputHelper.WriteLine($"   json: {JsonSerializer.Serialize(value)}");

        var union = ExpressionTypeUnion.FromObject(value);
        outputHelper.WriteLine($"   union: {union}");
        // Verify that the EqualsToString method throws an exception for unsupported types.
        Assert.Null(ExpressionEvaluator.ToStringForEquals(ExpressionTypeUnion.FromObject(value)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("null", null)]
    [InlineData("Null", null)]
    [InlineData("true", "true")]
    [InlineData("trUe", "true")]
    [InlineData("True", "true")]
    [InlineData(true, "true")]
    [InlineData("false", "false")]
    [InlineData("False", "false")]
    [InlineData("falSe", "false")]
    [InlineData(false, "false")]
    public void ToStringForEquals_SpecialCases(object? value, string? expected)
    {
        // Verify that the EqualsToString method returns the expected value for special cases.
        var toStringForEquals = ExpressionEvaluator.ToStringForEquals(ExpressionTypeUnion.FromObject(value));
        Assert.Equal(expected, toStringForEquals);
    }
}
