using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class ByteExtensions
{
    public static Address ToAddress(this byte[] bytes)
    {
        return new Address { Value = ByteString.CopyFrom(bytes) };
    }

    public static Hash ToHash(this byte[] bytes)
    {
        return Hash.LoadFromByteArray(bytes);
    }
}