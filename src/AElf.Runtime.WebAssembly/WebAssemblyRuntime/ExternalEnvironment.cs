using System.Security.Cryptography;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Types;
using Nethereum.Util;
using Secp256k1Net;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

public partial class ExternalEnvironment : IExternalEnvironment, ITransientDependency
{
    private readonly ICSharpContractReader _contractReader;
    private readonly IFeeService _feeService;

    public List<(byte[], byte[])> Events { get; } = new();
    public List<string> DebugMessages { get; set; } = new();
    public Address Caller => HostSmartContractBridgeContext?.Sender;
    public Address ContractAddress => HostSmartContractBridgeContext?.Self;
    public GasMeter GasMeter { get; set; }

    private IHostSmartContractBridgeContext? HostSmartContractBridgeContext { get; set; }

    public ExternalEnvironment(ICSharpContractReader contractReader, IFeeService feeService)
    {
        _contractReader = contractReader;
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
        
    }

    public long BlockNumber()
    {
        throw new NotImplementedException();
    }

    public int MaxValueSize()
    {
        return int.MaxValue;
    }

    public bool AppendDebugBuffer(string message)
    {
        return true;
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

    public Task ChargeGasAsync(RuntimeCosts runtimeCosts, Weight weight)
    {
        throw new NotImplementedException();
    }

    public async Task ChargeGasAsync(RuntimeCosts runtimeCosts, long size)
    {
        var balance = await _contractReader.GetBalanceAsync(Caller, Caller);
    }

    public void SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        HostSmartContractBridgeContext = smartContractBridgeContext;
        //Caller = smartContractBridgeContext.Sender;
        //ContractAddress = smartContractBridgeContext.Self!;
    }
}