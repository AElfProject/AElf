using NBitcoin.DataEncoders;

namespace AElf.Runtime.WebAssembly;

public class ExternalEnvironment : IExternalEnvironment
{
    public Dictionary<string, byte[]> Storage { get; set; } = new();

    public WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld)
    {
        var realKey = Encoders.Base58.EncodeData(key);
        WriteOutcome writeOutcome;
        if (Storage.TryGetValue(realKey, out var oldValue))
        {
            if (takeOld)
            {
                writeOutcome = new WriteOutcome
                    { WriteOutcomeType = WriteOutcomeType.Taken, Value = Encoders.Hex.EncodeData(oldValue) };
            }
            else
            {
                var length = oldValue.Length.ToString();
                if (oldValue.ToHex().All(c => c == '0'))
                {
                    length = "0";
                }

                writeOutcome = new WriteOutcome
                    { WriteOutcomeType = WriteOutcomeType.Overwritten, Value = length };
            }
        }
        else
        {
            writeOutcome = new WriteOutcome { WriteOutcomeType = WriteOutcomeType.New };
        }

        Storage[realKey] = value;
        return writeOutcome;
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