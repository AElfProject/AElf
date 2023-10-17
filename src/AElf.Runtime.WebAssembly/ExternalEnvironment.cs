using System.Security.Cryptography;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Types;
using Google.Protobuf;
using Nethereum.Util;
using Secp256k1Net;

namespace AElf.Runtime.WebAssembly;

public class ExternalEnvironment : IExternalEnvironment
{
    private readonly IFeeService _feeService;
    public Dictionary<string, ByteString> Writes { get; set; } = new();
    public Dictionary<string, bool> Reads { get; set; } = new();
    public Dictionary<string, bool> Deletes { get; set; } = new();
    public List<(byte[], byte[])> Events { get; } = new();
    public List<string> DebugMessages { get; set; } = new();
    public Address? Caller { get; set; }
    public GasMeter GasMeter { get; set; }

    public ExternalEnvironment(IFeeService feeService)
    {
        _feeService = feeService;
    }

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

    public void Terminate(Address beneficiary)
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

    public WriteOutcome SetStorage(byte[] key, byte[]? value, bool takeOld)
    {
        throw new NotImplementedException();
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

    public bool IsContract(byte[] address)
    {
        throw new NotImplementedException();
    }

    public Hash? CodeHash(byte[] address)
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

    public Address Address()
    {
        throw new NotImplementedException();
    }

    public long Balance()
    {
        throw new NotImplementedException();
    }

    public long GetWeightPrice(Weight weight)
    {
        return _feeService.CalculateFees(weight);
    }

    public long ValueTransferred()
    {
        throw new NotImplementedException();
    }

    public byte[]? EcdsaRecover(byte[] signature, byte[] messageHash)
    {
        try
        {
            CryptoHelper.RecoverPublicKey(signature, messageHash, out var pubkey);
            return pubkey;
        }
        catch (Exception)
        {
            throw new CryptographicException("Failed to perform ecdsa recover.");
        }
    }

    public long Now()
    {
        return TimestampHelper.GetUtcNow().Seconds;
    }

    public long MinimumBalance()
    {
        throw new NotImplementedException();
    }

    public (byte[], long) Random(byte[] subject)
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

    public byte[] EcdsaToEthAddress(byte[] pubkey)
    {
        if (pubkey.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            throw new ArgumentException("Incorrect pubkey size.");
        }
        var pubkeyNoPrefixCompressed = new byte[pubkey.Length - 1];
        Array.Copy(pubkey, 1, pubkeyNoPrefixCompressed, 0, pubkeyNoPrefixCompressed.Length);
        var initAddress = new Sha3Keccack().CalculateHash(pubkeyNoPrefixCompressed);
        var address = new byte[initAddress.Length - 12];
        Array.Copy(initAddress, 12, address, 0, initAddress.Length - 12);
        return address;
    }

    public void SetCodeHash(Hash hash)
    {
        throw new NotImplementedException();
    }

    public int ReentranceCount()
    {
        throw new NotImplementedException();
    }

    public int AccountReentranceCount(byte[] accountAddress)
    {
        throw new NotImplementedException();
    }

    public void AddDelegateDependency(Hash codeHash)
    {
        throw new NotImplementedException();
    }

    public void RemoveDelegateDependency(Hash codeHash)
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
        Caller = smartContractBridgeContext.Sender;
    }
}