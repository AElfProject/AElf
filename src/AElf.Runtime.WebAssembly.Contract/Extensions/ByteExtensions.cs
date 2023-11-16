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

    public static byte[] TrimZeroBytes(this byte[] bytes)
    {
        var result = new List<byte>();
        var zeroCountInEnd = 0;
        for (var i = bytes.Length() - 1; i >= 0; i--)
        {
            if (bytes[i] == 0)
            {
                zeroCountInEnd++;
            }
            else
            {
                break;
            }
        }

        var flag = true;
        foreach (var bye in bytes.Take(bytes.Length - zeroCountInEnd))
        {
            if (flag && bye == 0) continue;
            flag = false;
            result.Add(bye);
        }

        return result.ToArray();
    }
}