using System.Numerics;

namespace Scale.Decoders;

public class IntegerTypeDecoder
{
    public BigInteger Decode(byte[] bytes)
    {
        return new BigInteger(bytes);
    }
}