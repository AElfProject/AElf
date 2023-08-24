using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;

namespace AElf.Runtime.WebAssembly.Tests;

public class UnitTestExternalEnvironment : IExternalEnvironment
{
   
    public IHostSmartContractBridgeContext? HostSmartContractBridgeContext { get; set; }
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();

    public List<TransferEntry> Transfers { get; set; } = new();
    public Dictionary<Hash, byte[]> Events { get; set; } = new();
    public List<string> DebugMessages { get; set; } = new();
    public List<Hash> CodeHashes { get; set; } = new();

    public void Transfer(Address to, long value)
    {
        Transfers.Add(new TransferEntry { To = to, Value = value });
    }

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
        return true;
    }

    public Hash CodeHash(Address address)
    {
        return Hash.LoadFromByteArray(new byte[]
        {
            11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 11, 11, 11, 11, 11, 11,
        });
    }

    public Hash OwnCodeHash()
    {
        return Hash.LoadFromByteArray(new byte[]
        {
            10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10,
        });
    }

    public bool CallerIsOrigin()
    {
        return false;
    }

    public bool CallerIsRoot()
    {
        // &self.caller == &Origin::Root
        return false;
    }

    public Address GetAddress()
    {
        return TestAccounts.Bob;
    }

    public long Balance()
    {
        return 228;
    }

    public long ValueTransferred()
    {
        return 1337;
    }

    public Timestamp Now()
    {
        return Timestamp.FromDateTimeOffset(new DateTime(0, 0, 0, 0, 0, 1111));
    }

    public long MinimumBalance()
    {
        return 666;
    }

    public byte[] Random(byte[] subject)
    {
        return HashHelper.ComputeFrom(subject).ToByteArray();
    }

    public void DepositEvent(byte[] topics, byte[] data)
    {
        Events[HashHelper.ComputeFrom(topics)] = data;
    }

    public long BlockNumber()
    {
        return HostSmartContractBridgeContext!.CurrentHeight;
    }

    public int MaxValueSize()
    {
        return 16384;
    }

    public bool AppendDebugBuffer(string message)
    {
        DebugMessages.Add(message);
        return true;
    }

    public Address EcdsaToEthAddress(byte[] pk)
    {
        return TestAccounts.Bob;
    }

    public void SetCodeHash(Hash hash)
    {
        CodeHashes.Add(hash);
    }

    public int ReentranceCount()
    {
        return 12;
    }

    public int AccountReentranceCount(Address accountAddress)
    {
        return 12;
    }

    public long Nonce()
    {
        return 995;
    }

    public void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        HostSmartContractBridgeContext = smartContractBridgeContext;
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

public struct TransferEntry
{
    public Address To { get; set; }
    public long Value { get; set; }
}

public struct CallEntry
{
    
}