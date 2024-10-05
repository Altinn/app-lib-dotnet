using System.Numerics;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using FluentAssertions;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.LayoutExpressions.ExpressionEvaluatorTests;

public class EqualTests(ITestOutputHelper outputHelper)
{
    public static TheoryData<Type, object?> GetNumericTestData(double value) =>
        new()
        {
            { typeof(double), value },
            { typeof(byte), (byte)Math.Abs(value) },
            { typeof(sbyte), (sbyte)value },
            { typeof(short), (short)value },
            { typeof(ushort), (ushort)value },
            { typeof(int), (int)value },
            { typeof(uint), (uint)Math.Abs(value) },
            { typeof(long), (long)value },
            { typeof(ulong), (ulong)Math.Abs(value) },
            { typeof(float), (float)value },
            { typeof(decimal), (decimal)value },
            { typeof(double?), value },
            { typeof(byte?), (byte?)Math.Abs(value) },
            { typeof(sbyte?), (sbyte?)value },
            { typeof(short?), (short?)value },
            { typeof(ushort?), (ushort?)Math.Abs(value) },
            { typeof(int?), (int?)value },
            { typeof(uint?), (uint?)Math.Abs(value) },
            { typeof(long?), (long?)value },
            { typeof(ulong?), (ulong?)Math.Abs(value) },
            { typeof(float?), (float?)value },
            { typeof(decimal?), (decimal?)value },
            // (BigInteger)value, // Not supported by JsonSerializer
        };

    public static TheoryData<Type, object?> GetNullNumericData() =>
        new()
        {
            { typeof(double?), (double?)null },
            { typeof(byte?), (byte?)null },
            { typeof(sbyte?), (sbyte?)null },
            { typeof(short?), (short?)null },
            { typeof(ushort?), (ushort?)null },
            { typeof(int?), (int?)null },
            { typeof(uint?), (uint?)null },
            { typeof(long?), (long?)null },
            { typeof(ulong?), (ulong?)null },
            { typeof(float?), (float?)null },
            { typeof(decimal?), (decimal?)null },
        };

    public static TheoryData<Type, object> GetExoticTypes =>
        new()
        {
            { typeof(string), "123" },
            { typeof(bool), true },
            { typeof(bool), false },
            { typeof(string), "" },
            { typeof(DateTime), DateTime.Now },
            { typeof(DateOnly), DateOnly.FromDateTime(DateTime.Now) },
            { typeof(TimeOnly), TimeOnly.FromDateTime(DateTime.Now) },
        };

    [Theory]
    [MemberData(nameof(GetNumericTestData), 123.0)]
    [MemberData(nameof(GetNumericTestData), 0.5)]
    [MemberData(nameof(GetNumericTestData), -11.0)]
    [MemberData(nameof(GetNullNumericData))]
    [MemberData(nameof(GetExoticTypes))]
    public void ToStringForEquals_AgreesWithJsonSerializer(Type type, object? value)
    {
        outputHelper.WriteLine($"Object of type {type.Name}:");
        outputHelper.WriteLine($"   value:{value}");
        outputHelper.WriteLine($"   json: {JsonSerializer.Serialize(value)}");
        // Verify that the EqualsToString method returns the same value as the JsonSerializer.
        // apart from the issue of:
        var json = value switch
        {
            // null: Json returns "null", while ToStringForEquals returns C# null
            null => null,
            // strings: Json adds "" around the string, while ToStringForEquals does not
            string => value,
            // In remaining cases use JsonSerializer to get numbers ++ formatted as strings
            _ => JsonSerializer.Serialize(value)
        };

        var toStringForEquals = ExpressionEvaluator.ToStringForEquals(value);
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
                C = 3
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
        // Verify that the EqualsToString method throws an exception for unsupported types.
        Assert.Throws<NotImplementedException>(() => ExpressionEvaluator.ToStringForEquals(value));
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
        var toStringForEquals = ExpressionEvaluator.ToStringForEquals(value);
        Assert.Equal(expected, toStringForEquals);
    }
}
