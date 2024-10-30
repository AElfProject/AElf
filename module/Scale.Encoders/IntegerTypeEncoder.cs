using System.Numerics;
using Scale.Core;

namespace Scale.Encoders;

public class IntegerTypeEncoder
{
    public byte[] Encode(string value)
    {
        return !BigInteger.TryParse(value, out var bigInteger) ? [] : bigInteger.ToByteArray().RightPad(32);
    }

    public byte[] Encode(long value)
    {
        return new BigInteger(value).ToByteArray().RightPad(32);
    }

    public byte[] Encode(int value)
    {
        return new BigInteger(value).ToByteArray().RightPad(32);
    }

    public byte[] Encode(ulong value)
    {
        return new BigInteger(value).ToByteArray().RightPad(32);
    }

    public byte[] Encode(uint value)
    {
        return new BigInteger(value).ToByteArray().RightPad(32);
    }

    public byte[] Encode(BigInteger value)
    {
        return value.ToByteArray().RightPad(32);
    }
}