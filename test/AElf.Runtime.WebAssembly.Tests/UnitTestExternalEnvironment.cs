using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using AElf.Runtime.WebAssembly;

namespace AElf.Runtime.WebAssembly.Tests;

public class UnitTestExternalEnvironment : IExternalEnvironment, ITransientDependency
{
    private readonly ICSharpContractReader _contractReader;
    private IHostSmartContractBridgeContext? HostSmartContractBridgeContext { get; set; }
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();

    public List<TransferEntry> Transfers { get; set; } = new();
    public List<CallEntry> Calls { get; set; } = new();
    public List<CallCodeEntry> DelegateCalls { get; set; } = new();
    public List<InstantiateEntry> Instantiates { get; set; } = new();
    public List<TerminationEntry> Terminations { get; set; } = new();
    public List<Hash> DelegateDependencies { get; set; } = new();
    public Tuple<byte[], byte[]> EcdsaRecover { get; set; }
    public Tuple<byte[], byte[], byte[]> Sr25519Verify { get; set; }
    public List<(byte[], byte[])> Events { get; set; } = new();
    public List<string> DebugMessages { get; set; } = new();
    public List<Hash> CodeHashes { get; set; } = new();
    public Address? Caller { get; set; } = WebAssemblyRuntimeTestConstants.Alice;
    public GasMeter GasMeter { get; set; }

    public ExecuteReturnValue Call(Weight gasLimit, long depositLimit, Address to, long value, byte[] inputData,
        bool allowReentry)
    {
        Calls.Add(new CallEntry(to, value, inputData, allowReentry));
        return new ExecuteReturnValue
        {
            Flags = ReturnFlags.Empty,
            Data = WebAssemblyRuntimeTestConstants.CallReturnData
        };
    }

    public ExecuteReturnValue DelegateCall(Hash codeHash, byte[] data)
    {
        DelegateCalls.Add(new CallCodeEntry(codeHash, data));
        return new ExecuteReturnValue
        {
            Flags = ReturnFlags.Empty,
            Data = WebAssemblyRuntimeTestConstants.CallReturnData
        };
    }
    public (Address, ExecuteReturnValue) Instantiate(Weight gasLimit, long depositLimit, Hash codeHash, long value,
        byte[] inputData, byte[] salt)
    {
        Instantiates.Add(new InstantiateEntry(codeHash, value, inputData, gasLimit.RefTime, salt));
        return (
            new AddressGenerator().GenerateContractAddress(WebAssemblyRuntimeTestConstants.Alice, codeHash, inputData,
                salt),
            new ExecuteReturnValue
            {
                Flags = ReturnFlags.Empty,
                Data = Array.Empty<byte>()
            });
    }

    public void Terminate(Address beneficiary)
    {
        Terminations.Add(new TerminationEntry(beneficiary));
    }

    public void Transfer(Address to, long value)
    {
        Transfers.Add(new TransferEntry(to, value));
    }

    public WriteOutcome SetStorage(byte[] key, byte[]? value, bool takeOld)
    {
        var stateKey = GetStateKey(key);
        WriteOutcome writeOutcome;
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

    public bool IsContract(byte[] address)
    {
        return true;
    }

    public Hash? CodeHash(byte[] address)
    {
        var codeHash = _contractReader.GetContractHashAsync(Types.Address.FromBytes(address), Types.Address.FromBytes(address)).Result;
        return codeHash;
    }

    public Hash OwnCodeHash() 
    {
        var address = HostSmartContractBridgeContext!.Sender.ToByteArray();
        var codeHash = _contractReader.GetContractHashAsync(Types.Address.FromBytes(address), Types.Address.FromBytes(address)).Result;
        return codeHash;
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

    public byte[] Address()
    {
        return WebAssemblyRuntimeTestConstants.Bob.ToByteArray();
    }

    public long Balance()
    {
        return 228;
    }

    public long GetWeightPrice(Weight weight)
    {
        return new FeeService(new IFeeProvider[]
            {
                new LengthFeeProvider(),
                new WeightFeeProvider(new FeeFunctionProvider())
            })
            .CalculateFees(weight);
    }

    public long ValueTransferred()
    {
        return 1337;
    }

    byte[] IExternalEnvironment.EcdsaRecover(byte[] signature, byte[] messageHash)
    {
        EcdsaRecover = new Tuple<byte[], byte[]>(signature, messageHash);
        
        var publicKey = new byte[]
        {
            4, 167, 102, 223, 221, 190, 59, 84, 106, 86, 216, 34, 156, 169, 185, 104, 232, 226, 9, 180, 172, 204, 177,
            252, 241, 189, 45, 73, 138, 249, 13, 145, 181, 132, 10, 60, 115, 219, 136, 189, 147, 213, 66, 89, 147, 228,
            175, 128, 169, 55, 137, 128, 193, 212, 225, 100, 224, 49, 116, 233, 242, 198, 73, 17, 149
        };
        return publicKey;
    }

    public long Now()
    {
        return 1111;
    }

    public long MinimumBalance()
    {
        return 666;
    }

    public (byte[], long) Random(byte[] subject)
    {
        return (subject, 42);
    }

    public void DepositEvent(byte[] topics, byte[] data)
    {
        Events.Add((topics, data));
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

    public byte[] EcdsaToEthAddress(byte[] pubkey)
    {
        return WebAssemblyRuntimeTestConstants.Bob.ToByteArray().Take(20).ToArray();
    }

    public void SetCodeHash(Hash hash)
    {
        CodeHashes.Add(hash);
    }

    public int ReentranceCount()
    {
        return 12;
    }

    public int AccountReentranceCount(byte[] accountAddress)
    {
        return 12;
    }

    public void AddDelegateDependency(Hash codeHash)
    {
        DelegateDependencies.Add(codeHash);
    }

    public void RemoveDelegateDependency(Hash codeHash)
    {
        DelegateDependencies.Remove(codeHash);
    }

    public long Nonce()
    {
        return 995;
    }

    public Task ChargeGasAsync(RuntimeCosts runtimeCosts, Weight weight)
    {
        return Task.CompletedTask;
    }
    
    public Task ChargeGasAsync(RuntimeCosts runtimeCosts, long size)
    {
        return Task.CompletedTask;
    }

    public void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        HostSmartContractBridgeContext = smartContractBridgeContext;
    }

    private string GetStateKey(byte[] key)
    {
        return new ScopedStatePath
        {
            Address = Types.Address.FromBase58("2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz"),
            Path = new StatePath
            {
                Parts = { key.ToPlainBase58() }
            }
        }.ToStateKey();
    }

    private void AddExceptionToDebugMessage(Exception ex)
    {
        AppendDebugBuffer(ex.Message);
#if DEBUG
        AppendDebugBuffer(ex.ToString());
#endif
    }
}

public record TransferEntry(Address To, long Value);
public record CallEntry(Address To, long Value, byte[] Data, bool AllowReentry);
public record CallCodeEntry(Hash CodeHash, byte[] Data);
public record InstantiateEntry(Hash CodeHash, long Value, byte[] Data, long GasLeft, byte[] Salt);
public record TerminationEntry(Address Beneficiary);