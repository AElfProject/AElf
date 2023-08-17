using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using NBitcoin.DataEncoders;

namespace AElf.Runtime.WebAssembly;

public class UnitTestExternalEnvironment : IExternalEnvironment
{
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();

    public WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld)
    {
        var stateKey = GetStateKey(key);
        WriteOutcome writeOutcome;
        if (Writes.TryGetValue(stateKey, out var oldValue))
        {
            if (takeOld)
            {
                writeOutcome = new WriteOutcome
                {
                    WriteOutcomeType = WriteOutcomeType.Taken, Value = Encoders.Hex.EncodeData(oldValue.ToByteArray())
                };
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

        Writes[stateKey] = ByteString.CopyFrom(value);
        return writeOutcome;
    }

    public async Task<byte[]?> GetStorageAsync(byte[] key)
    {
        var stateKey = GetStateKey(key);
        if (Writes.ContainsKey(stateKey))
        {
            if (Writes.TryGetValue(stateKey, out var byteStringValue))
            {
                Reads.TryAdd(stateKey, true);
                return byteStringValue.ToByteArray();
            }
        }

        return null;
    }

    public void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
    }

    private string GetStateKey(byte[] key)
    {
        return new ScopedStatePath
        {
            Address = Address.FromBase58("2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz"),
            Path = new StatePath
            {
                Parts = { key.ToPlainBase58() }
            }
        }.ToStateKey();
    }
}