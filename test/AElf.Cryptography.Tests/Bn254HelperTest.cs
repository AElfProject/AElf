using AElf.Cryptography.Bn254;
using AElf.Types;
using Bn254.Net;
using Shouldly;
using Xunit;
using NetBn254 = Bn254.Net;

namespace AElf.Cryptography.Tests;

public class EdDsaHelperTest
{
    public static byte[] ToBytes32(BigIntValue value)
    {
        var bytes = value.ToBigEndianBytes();
        var newArray = new byte[32];
        for (int i = 0; i < bytes.Length; i++)
        {
            newArray[31 - i] = bytes[bytes.Length - 1 - i];
        }

        return newArray;
    }

    [Fact]
    public void Bn254G1Mul_Test()
    {
        // Arrange
        byte[] x1 = ToBytes32(new BigIntValue(0));
        byte[] y1 = ToBytes32(new BigIntValue(0));
        byte[] scalar = ToBytes32(new BigIntValue(0));

        // use raw api to compute result
        var (expectedXuInt256, expectedYuInt256) = NetBn254.Bn254.Mul(
            UInt256.FromBigEndianBytes(x1),
            UInt256.FromBigEndianBytes(y1),
            UInt256.FromBigEndianBytes(scalar)
        );
        var expectedX = expectedXuInt256.ToBigEndianBytes();
        var expectedY = expectedYuInt256.ToBigEndianBytes();

        // Act
        var (xResult, yResult) = Bn254Helper.Bn254G1Mul(x1, y1, scalar);

        // Assert
        xResult.ShouldBe(expectedX);
        yResult.ShouldBe(expectedY);
    }
    
    [Fact]
    public void Bn254G1Add_Test()
    {
        // Arrange
        byte[] x1 = ToBytes32(new BigIntValue(0));
        byte[] y1 = ToBytes32(new BigIntValue(0));
        byte[] x2 = ToBytes32(new BigIntValue(0));
        byte[] y2 = ToBytes32(new BigIntValue(0));

        // Use raw API to compute expected results
        var (expectedX3UInt256, expectedY3UInt256) = NetBn254.Bn254.Add(
            UInt256.FromBigEndianBytes(x1),
            UInt256.FromBigEndianBytes(y1),
            UInt256.FromBigEndianBytes(x2),
            UInt256.FromBigEndianBytes(y2)
        );
        var expectedX3 = expectedX3UInt256.ToBigEndianBytes();
        var expectedY3 = expectedY3UInt256.ToBigEndianBytes();

        // Act
        var (x3Result, y3Result) = Bn254Helper.Bn254G1Add(x1, y1, x2, y2);

        // Assert
        x3Result.ShouldBe(expectedX3);
        y3Result.ShouldBe(expectedY3);
    }
    
    [Fact]
    public void Bn254Pairing_Test()
    {
        // Arrange
        var input = new (byte[], byte[], byte[], byte[], byte[], byte[])[]
        {
            (
                ToBytes32(new BigIntValue(0)),
                ToBytes32(new BigIntValue(0)),
                ToBytes32(new BigIntValue(0)),
                ToBytes32(new BigIntValue(0)),
                ToBytes32(new BigIntValue(0)),
                ToBytes32(new BigIntValue(0))
            )
        };

        // Use raw API to compute expected results
        bool expected = NetBn254.Bn254.Pairing(new (UInt256, UInt256, UInt256, UInt256, UInt256, UInt256)[]
        {
            (
                UInt256.FromBigEndianBytes(input[0].Item1),
                UInt256.FromBigEndianBytes(input[0].Item2),
                UInt256.FromBigEndianBytes(input[0].Item3),
                UInt256.FromBigEndianBytes(input[0].Item4),
                UInt256.FromBigEndianBytes(input[0].Item5),
                UInt256.FromBigEndianBytes(input[0].Item6)
            )
        });

        // Act
        bool result = Bn254Helper.Bn254Pairing(input);

        // Assert
        result.ShouldBe(expected);
    }
}