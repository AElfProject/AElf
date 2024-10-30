using System.Numerics;
using AElf;

namespace Scale;

public class IntType : Int256Type
{
    public static IntType From(BigInteger value)
    {
        var instance = new IntType();
        instance.Create(value);
        return instance;
    }

    public static IntType From(long value)
    {
        var instance = new IntType();
        instance.Create(value);
        return instance;
    }

    public static IntType From(byte[] value)
    {
        var instance = new IntType();
        instance.Create(value);
        return instance;
    }

    public static IntType From(string value)
    {
        return From(ByteArrayHelper.HexStringToByteArray(value));
    }
}