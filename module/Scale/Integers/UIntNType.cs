using System.Numerics;

namespace Scale;

/// <summary>
/// TODO: Not finished implementation yet.
/// </summary>
public class UIntNType : PrimitiveType<BigInteger>
{
    public override string TypeName => "uintN";

    public UIntNType(byte n)
    {
        if (n < 8 || n % 8 != 0)
        {
            throw new NotSupportedException("N can only be anything between 8 and 256 bits and a multiple of 8.");
        }

        TypeSize = n;
    }
    
    public UIntNType(byte n, BigInteger value)
    {
        if (n < 8 || n % 8 != 0)
        {
            throw new NotSupportedException("N can only be anything between 8 and 256 bits and a multiple of 8.");
        }

        TypeSize = n;
        Create(value);
    }

    public override byte[] Encode()
    {
        throw new NotSupportedException();
    }

    public override void Create(BigInteger value)
    {
        throw new NotSupportedException();
    }
    
    public static byte[] GetBytesFrom(byte n, ulong value)
    {
        var bytes = new byte[n];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }
}