using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.CSharp.Core;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Google.Protobuf.Collections;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract;

public class HostSmartContractBridgeContextOptions
{
    public Dictionary<string, string> ContextVariables { get; set; } = new();
}

public class HostSmartContractBridgeContext : IHostSmartContractBridgeContext, ITransientDependency
{
    private readonly IAccountService _accountService;

    private readonly Lazy<ICachedStateProvider> _lazyStateProvider;
    private readonly ISmartContractBridgeService _smartContractBridgeService;
    private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

    private ITransactionContext _transactionContext;

    public HostSmartContractBridgeContext(ISmartContractBridgeService smartContractBridgeService,
        ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, IAccountService accountService,
        IOptionsSnapshot<HostSmartContractBridgeContextOptions> options)
    {
        _smartContractBridgeService = smartContractBridgeService;
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        _accountService = accountService;

        Variables = new ContextVariableDictionary(options.Value.ContextVariables);

        var self = this;

        Address GetAddress()
        {
            return self.Transaction.To;
        }

        _lazyStateProvider = new Lazy<ICachedStateProvider>(
            () => new CachedStateProvider(
                new ScopedStateProvider
                {
                    ContractAddress = GetAddress(),
                    HostSmartContractBridgeContext = this
                }),
            LazyThreadSafetyMode.PublicationOnly);
    }

    private ICachedStateProvider CachedStateProvider => _lazyStateProvider.Value;

    public Transaction Transaction => TransactionContext.Transaction.Clone();

    public ITransactionContext TransactionContext
    {
        get => _transactionContext;
        set
        {
            _transactionContext = value;
            CachedStateProvider.Cache = _transactionContext?.StateCache ?? new NullStateCache();
        }
    }

    public IStateProvider StateProvider => _lazyStateProvider.Value;

    public Address GetContractAddressByName(string hash)
    {
        var chainContext = new ChainContext
        {
            BlockHash = TransactionContext.PreviousBlockHash,
            BlockHeight = TransactionContext.BlockHeight - 1,
            StateCache = CachedStateProvider.Cache
        };
        return AsyncHelper.RunSync(() =>
            _smartContractBridgeService.GetAddressByContractNameAsync(chainContext, hash));
    }

    public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
    {
        var chainContext = new ChainContext
        {
            BlockHash = TransactionContext.PreviousBlockHash,
            BlockHeight = TransactionContext.BlockHeight - 1,
            StateCache = CachedStateProvider.Cache
        };
        return AsyncHelper.RunSync(() =>
            _smartContractBridgeService.GetSystemContractNameToAddressMappingAsync(chainContext));
    }

    public void Initialize(ITransactionContext transactionContext)
    {
        TransactionContext = transactionContext;
    }

    public async Task<ByteString> GetStateAsync(string key)
    {
        return await _smartContractBridgeService.GetStateAsync(
            Self, key, CurrentHeight - 1, PreviousBlockHash);
    }

    public int ChainId => _smartContractBridgeService.GetChainId();
    public ContextVariableDictionary Variables { get; }

    public void LogDebug(Func<string> func)
    {
#if DEBUG
        _smartContractBridgeService.LogDebug(() =>
            $"TX = {Transaction?.GetHash().ToHex()}, Method = {Transaction?.MethodName}, {func()}");
#endif
    }

    public void FireLogEvent(LogEvent logEvent)
    {
        TransactionContext.Trace.Logs.Add(logEvent);
    }

    public Hash GenerateId(Address contractAddress, IEnumerable<byte> bytes)
    {
        var contactedBytes = OriginTransactionId.Value.Concat(contractAddress.Value);
        var enumerable = bytes as byte[] ?? bytes?.ToArray();
        if (enumerable != null)
            contactedBytes = contactedBytes.Concat(enumerable);
        return HashHelper.ComputeFrom(contactedBytes.ToArray());
    }

    public object ValidateStateSize(object obj)
    {
        var stateSizeLimit = AsyncHelper.RunSync(() => _smartContractBridgeService.GetStateSizeLimitAsync(
            new ChainContext
            {
                BlockHash = _transactionContext.PreviousBlockHash,
                BlockHeight = _transactionContext.BlockHeight - 1
            }));
        var size = SerializationHelper.Serialize(obj).Length;
        if (size > stateSizeLimit)
            throw new StateOverSizeException($"State size {size} exceeds limit of {stateSizeLimit}.");
        return obj;
    }

    public Hash GetRandomHash(Hash fromHash)
    {
        var currentBlockTimeHash = HashHelper.ComputeFrom(CurrentBlockTime);
        return HashHelper.XorAndCompute(TransactionId, HashHelper.XorAndCompute(currentBlockTimeHash,
            HashHelper.XorAndCompute(fromHash, PreviousBlockHash)));
    }

    public long ConvertHashToInt64(Hash hash, long start = 0, long end = long.MaxValue)
    {
        if (start < 0 || start > end) throw new ArgumentException("Incorrect arguments.");

        var range = end.Sub(start);
        var bigInteger = new BigInteger(hash.Value.ToByteArray());
        // This is safe because range is long type.
        var index = Math.Abs((long)(bigInteger % range));
        return index.Add(start);
    }

    public Hash TransactionId => TransactionContext.Transaction.GetHash();
    public Address Sender => TransactionContext.Transaction.From.Clone();
    public Address Self => TransactionContext.Transaction.To.Clone();
    public Address Origin => TransactionContext.Origin.Clone();
    public Hash OriginTransactionId => TransactionContext.OriginTransactionId;
    public long CurrentHeight => TransactionContext.BlockHeight;
    public Timestamp CurrentBlockTime => TransactionContext.CurrentBlockTime;
    public Hash PreviousBlockHash => TransactionContext.PreviousBlockHash.Clone();

    /// <summary>
    ///     Recovers the first public key signing this transaction.
    /// </summary>
    /// <returns>Public key byte array</returns>
    public byte[] RecoverPublicKey()
    {
        return RecoverPublicKey(TransactionContext.Transaction.Signature.ToByteArray(),
            TransactionContext.Transaction.GetHash().ToByteArray());
    }

    public T Call<T>(Address fromAddress, Address toAddress, string methodName, ByteString args)
        where T : IMessage<T>, new()
    {
        var trace = AsyncHelper.RunSync(async () =>
        {
            var chainContext = new ChainContext
            {
                BlockHash = TransactionContext.PreviousBlockHash,
                BlockHeight = TransactionContext.BlockHeight - 1,
                StateCache = CachedStateProvider.Cache
            };

            var tx = new Transaction
            {
                From = fromAddress,
                To = toAddress,
                MethodName = methodName,
                Params = args
            };
            return await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, CurrentBlockTime);
        });

        if (!trace.IsSuccessful()) throw new ContractCallException(trace.Error);

        var obj = new T();
        obj.MergeFrom(trace.ReturnValue);
        return obj;
    }

    public void SendInline(Address toAddress, string methodName, ByteString args)
    {
        TransactionContext.Trace.InlineTransactions.Add(new Transaction
        {
            From = Self,
            To = toAddress,
            MethodName = methodName,
            Params = args
        });
    }

    public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName,
        ByteString args)
    {
        TransactionContext.Trace.InlineTransactions.Add(new Transaction
        {
            From = ConvertVirtualAddressToContractAddress(fromVirtualAddress, Self),
            To = toAddress,
            MethodName = methodName,
            Params = args
        });
    }

    public void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName,
        ByteString args)
    {
        TransactionContext.Trace.InlineTransactions.Add(new Transaction
        {
            From = ConvertVirtualAddressToContractAddressWithContractHashName(fromVirtualAddress, Self),
            To = toAddress,
            MethodName = methodName,
            Params = args
        });
    }

    public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress, Address contractAddress)
    {
        return Address.FromPublicKey(contractAddress.Value.Concat(
            virtualAddress.Value.ToByteArray().ComputeHash()).ToArray());
    }

    public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress,
        Address contractAddress)
    {
        var systemHashName = GetSystemContractNameToAddressMapping().First(kv => kv.Value == contractAddress).Key;
        return Address.FromPublicKey(systemHashName.Value.Concat(virtualAddress.Value.ToByteArray().ComputeHash())
            .ToArray());
    }

    public Address GetZeroSmartContractAddress()
    {
        return _smartContractBridgeService.GetZeroSmartContractAddress();
    }

    public Address GetZeroSmartContractAddress(int chainId)
    {
        return _smartContractBridgeService.GetZeroSmartContractAddress(chainId);
    }

    public List<Transaction> GetPreviousBlockTransactions()
    {
        return AsyncHelper.RunSync(() => _smartContractBridgeService.GetBlockTransactions(
            TransactionContext.PreviousBlockHash));
    }

    public bool VerifySignature(Transaction tx)
    {
        return tx.VerifySignature();
    }

    public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
    {
        if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress())) throw new NoPermissionException();

        var contractDto = new ContractDto
        {
            BlockHeight = CurrentHeight,
            ContractAddress = address,
            SmartContractRegistration = registration,
            ContractName = name,
            IsPrivileged = false
        };

        AsyncHelper.RunSync(() => _smartContractBridgeService.DeployContractAsync(contractDto));
    }

    public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
    {
        if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress())) throw new NoPermissionException();

        var contractDto = new ContractDto
        {
            BlockHeight = CurrentHeight,
            ContractAddress = address,
            SmartContractRegistration = registration,
            ContractName = null,
            IsPrivileged = false
        };
        AsyncHelper.RunSync(() => _smartContractBridgeService.UpdateContractAsync(contractDto));
    }

    public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
    {
        var cabBeRecovered = CryptoHelper.RecoverPublicKey(signature, hash, out var publicKey);
        return !cabBeRecovered ? null : publicKey;
    }
}