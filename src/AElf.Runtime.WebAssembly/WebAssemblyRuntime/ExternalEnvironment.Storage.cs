using AElf.Runtime.WebAssembly.Extensions;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly;

public partial class ExternalEnvironment
{
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();

    public WriteOutcome SetStorage(byte[] key, byte[]? value, bool takeOld)
    {
        WriteOutcome writeOutcome;
        var stateKey = key.ToStateKey(ContractAddress);
        if (Writes.TryGetValue(stateKey, out var oldValue))
        {
            if (takeOld)
            {
                writeOutcome = new WriteOutcome
                {
                    WriteOutcomeType = WriteOutcomeType.Taken,
                    Value = oldValue.ToHex()
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
                {
                    WriteOutcomeType = WriteOutcomeType.Overwritten,
                    Value = length
                };
            }

            if (value != null)
            {
                Writes[stateKey] = ByteString.CopyFrom(value);
            }
            else
            {
                Writes.Remove(stateKey);
                Deletes.Add(stateKey, true);
            }
        }
        else
        {
            writeOutcome = new WriteOutcome { WriteOutcomeType = WriteOutcomeType.New };
            if (value != null)
            {
                Writes[stateKey] = ByteString.CopyFrom(value);
            }
        }

        return writeOutcome;
    }

    public async Task<byte[]?> GetStorageAsync(byte[] key)
    {
        var stateKey = key.ToStateKey(ContractAddress);
        if (Writes.ContainsKey(stateKey))
        {
            if (Writes.TryGetValue(stateKey, out var byteStringValue))
            {
                Reads.TryAdd(stateKey, true);
                return byteStringValue.ToByteArray();
            }
        }

        var value = await HostSmartContractBridgeContext!.GetStateAsync(stateKey);
        var byteArrayValue = value?.ToByteArray();
        Reads.TryAdd(stateKey, value != null);
        return byteArrayValue;
    }

    public async Task<int> GetStorageSizeAsync(byte[] key)
    {
        var value = await GetStorageAsync(key);
        return value?.Length ?? 0;
    }
}