using System;
using System.Numerics;
using Xunit;

namespace AElf.Types.Tests;

public class BigIntegerTest
{
    [Fact]
    public void Zero_Should_Be_Zero()
    {
        var zero = BigIntValue.Zero;
        Assert.Equal("0", zero.Value);
    }

    [Fact]
    public void One_Should_Be_One()
    {
        var one = BigIntValue.One;
        Assert.Equal("1", one.Value);
    }

    [Fact]
    public void BigIntValue_FromBigEndianBytes_Should_Work()
    {
        var bytes = new byte[] { 0x01, 0x00 }; // 256 in big endian
        var bigIntValue = BigIntValue.FromBigEndianBytes(bytes);
        Assert.Equal("256", bigIntValue.Value);
    }

    [Fact]
    public void ToBigEndianBytes_Should_Work()
    {
        var bigIntValue = new BigIntValue { Value = "256" };
        var bytes = bigIntValue.ToBigEndianBytes();
        Assert.Equal(new byte[] { 0x01, 0x00 }, bytes);
    }

    [Theory]
    [InlineData("123", "123", 0)]
    [InlineData("123", "456", -1)]
    [InlineData("456", "123", 1)]
    public void CompareTo_Should_Correctly_Compare(string a, string b, int expected)
    {
        var bigIntValueA = new BigIntValue { Value = a };
        var bigIntValueB = new BigIntValue { Value = b };

        Assert.Equal(expected, bigIntValueA.CompareTo(bigIntValueB));
    }

    [Fact]
    public void Implicit_Conversion_From_String_Should_Work()
    {
        BigIntValue bigIntValue = "123";
        Assert.Equal("123", bigIntValue.Value);
    }

    [Fact]
    public void Implicit_Conversion_From_Long_Should_Work()
    {
        BigIntValue bigIntValue = 456L;
        Assert.Equal("456", bigIntValue.Value);
    }

    [Fact]
    public void Addition_Operator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "456";
        BigIntValue result = a + b;
        Assert.Equal("579", result.Value);
    }

    [Fact]
    public void Subtraction_Operator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "23";
        BigIntValue result = a - b;
        Assert.Equal("100", result.Value);
    }

    [Fact]
    public void Multiplication_Operator_Should_Work()
    {
        BigIntValue a = "10";
        BigIntValue b = "20";
        BigIntValue result = a * b;
        Assert.Equal("200", result.Value);
    }

    [Fact]
    public void Modulus_Operator_Should_Work()
    {
        BigIntValue a = "10";
        BigIntValue b = "3";
        BigIntValue result = a % b;
        Assert.Equal("1", result.Value);
    }

    [Fact]
    public void Equality_Operator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "123";
        Assert.True(a == b);

        BigIntValue c = "456";
        Assert.False(a == c);
    }

    [Fact]
    public void Inequality_Operator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "456";
        Assert.True(a != b);

        BigIntValue c = "123";
        Assert.False(a != c);
    }

    [Fact]
    public void LessThanOperator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "456";
        Assert.True(a < b);
        Assert.False(b < a);
    }

    [Fact]
    public void GreaterThanOperator_Should_Work()
    {
        BigIntValue a = "456";
        BigIntValue b = "123";
        Assert.True(a > b);
        Assert.False(b > a);
    }

    [Fact]
    public void LessThanOrEqualOperator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "123";
        BigIntValue c = "456";
        Assert.True(a <= b);
        Assert.True(a <= c);
        Assert.False(c <= a);
    }

    [Fact]
    public void GreaterThanOrEqualOperator_Should_Work()
    {
        BigIntValue a = "123";
        BigIntValue b = "123";
        BigIntValue c = "456";
        Assert.True(a >= b);
        Assert.True(c >= a);
        Assert.False(a >= c);
    }
    
    [Theory]
    [InlineData("0", 0)]
    [InlineData("123456789", 123456789)]
    [InlineData("-987654321", -987654321)]
    [InlineData("1_000_000_000", 1000000000)]
    [InlineData("-1_234_567", -1234567)]
    public void ConvertStringToBigInteger_ValidStrings_Should_ParseCorrectly(string input, long expected)
    {
        BigIntValue bigIntValue = input;
        BigInteger result = bigIntValue;  // Implicit conversion to BigInteger
        Assert.Equal(new BigInteger(expected), result);
    }

}