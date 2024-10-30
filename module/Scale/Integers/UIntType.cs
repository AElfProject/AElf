using System.Numerics;
using AElf;

namespace Scale;

public class UIntType : UInt256Type
{
    public static UIntType From(BigInteger value)
    {
        var instance = new UIntType();
        instance.Create(value);
        return instance;
    }

    public static UIntType From(ulong value)
    {
        var instance = new UIntType();
        instance.Create(value);
        return instance;
    }

    public static UIntType From(byte[] value)
    {
        var instance = new UIntType();
        instance.Create(value);
        return instance;
    }

    public static UIntType From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}