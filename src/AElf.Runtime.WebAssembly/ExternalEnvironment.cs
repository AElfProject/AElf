using NBitcoin.DataEncoders;

namespace AElf.Runtime.WebAssembly;

public class ExternalEnvironment : IExternalEnvironment
{
    public Dictionary<string, byte[]> Storage { get; set; } = new();

    public void SetStorage(byte[] key, byte[] value, bool takeOld)
    {
        Storage[Encoders.Base58.EncodeData(key)] = value;
    }

    public bool TryGetStorage(byte[] key, out byte[] value)
    {
        var keyStr = Encoders.Base58.EncodeData(key);
        if (Storage.ContainsKey(keyStr))
        {
            if (Storage.TryGetValue(keyStr, out value!))
            {
                return true;
            }
        }

        value = Array.Empty<byte>();
        return false;
    }
}