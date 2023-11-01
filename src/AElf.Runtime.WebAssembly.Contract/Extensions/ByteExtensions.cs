using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

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

    public static string ToStateKey(this byte[] key, Address contractAddress)
    {
        return new ScopedStatePath
        {
            Address = contractAddress,
            Path = new StatePath
            {
                Parts = { key.ToPlainBase58() }
            }
        }.ToStateKey();
    }
}