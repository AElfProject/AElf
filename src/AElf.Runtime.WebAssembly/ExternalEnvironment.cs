using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;

namespace AElf.Runtime.WebAssembly;

public class ExternalEnvironment : IExternalEnvironment
{
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();
    public Dictionary<Hash, byte[]> Events { get; } = new();
    public List<string> DebugMessages { get; set; } = new();

    public ExecuteReturnValue Call(Weight gasLimit, long depositLimit, Address to, long value, byte[] inputData,
        bool allowReentry)
    {
        throw new NotImplementedException();
    }

    public ExecuteReturnValue DelegateCall(Hash codeHash, byte[] data)
    {
        throw new NotImplementedException();
    }

    public (Address, ExecuteReturnValue) Instantiate(Weight gasLimit, long depositLimit, Hash codeHash, long value,
        byte[] inputData, byte[] salt)
    {
        throw new NotImplementedException();
    }

    public void Transfer(Address to, long value)
    {
        throw new NotImplementedException();
    }

    public byte[] GetStorage(Key key)
    {
        throw new NotImplementedException();
    }

    public int GetStorageSize(Key key)
    {
        throw new NotImplementedException();
    }

    public IHostSmartContractBridgeContext? HostSmartContractBridgeContext { get; set; }

    public WriteOutcome SetStorage(byte[] key, byte[] value, bool takeOld)
    {
        var stateKey = GetStateKey(key);
        WriteOutcome writeOutcome;
        if (Writes.TryGetValue(stateKey, out var oldValue))
        {
            if (takeOld)
            {
                writeOutcome = new WriteOutcome
                    { WriteOutcomeType = WriteOutcomeType.Taken, Value = Encoders.Hex.EncodeData(oldValue.ToByteArray()) };
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

    public Address Caller()
    {
        return HostSmartContractBridgeContext!.Sender;
    }

    public bool IsContract()
    {
        throw new NotImplementedException();
    }

    public Hash CodeHash(Address address)
    {
        throw new NotImplementedException();
    }

    public Hash OwnCodeHash()
    {
        throw new NotImplementedException();
    }

    public bool CallerIsOrigin()
    {
        throw new NotImplementedException();
    }

    public bool CallerIsRoot()
    {
        throw new NotImplementedException();
    }

    public Address GetAddress()
    {
        throw new NotImplementedException();
    }

    public long Balance()
    {
        throw new NotImplementedException();
    }

    public long ValueTransferred()
    {
        throw new NotImplementedException();
    }

    public Timestamp Now()
    {
        throw new NotImplementedException();
    }

    public long MinimumBalance()
    {
        throw new NotImplementedException();
    }

    public byte[] Random(byte[] subject)
    {
        throw new NotImplementedException();
    }

    public void DepositEvent(byte[] topics, byte[] data)
    {
        throw new NotImplementedException();
    }

    public long BlockNumber()
    {
        throw new NotImplementedException();
    }

    public int MaxValueSize()
    {
        throw new NotImplementedException();
    }

    public bool AppendDebugBuffer(string message)
    {
        throw new NotImplementedException();
    }

    public Address EcdsaToEthAddress(byte[] pk)
    {
        throw new NotImplementedException();
    }

    public void SetCodeHash(Hash hash)
    {
        throw new NotImplementedException();
    }

    public int ReentranceCount()
    {
        throw new NotImplementedException();
    }

    public int AccountReentranceCount(Address accountAddress)
    {
        throw new NotImplementedException();
    }

    public long Nonce()
    {
        throw new NotImplementedException();
    }

    private string GetStateKey(byte[] key)
    {
        return new ScopedStatePath
        {
            Address = HostSmartContractBridgeContext!.Self,
            Path = new StatePath
            {
                Parts = { key.ToPlainBase58() }
            }
        }.ToStateKey();
    }

    public void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        HostSmartContractBridgeContext = smartContractBridgeContext;
    }
}