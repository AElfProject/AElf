using Google.Protobuf;
using Nethereum.ABI;

namespace AElf.Runtime.WebAssembly.Types;

public static class CommonExtension
{
    public static ByteString ToParameter(this ABIValue abiValue)
    {
        var bytes = new ABIEncode().GetABIEncodedPacked(abiValue);
        return ByteString.CopyFrom(bytes);
    }

    public static ABIValue ToABIValue(this byte[] bytes)
    {
        return new ABIValue("bytes", bytes);
    }
}