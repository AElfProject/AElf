using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract
{
    public class HostSmartContractBridgeContext : IHostSmartContractBridgeContext, ITransientDependency
    {
        private readonly ISmartContractBridgeService _smartContractBridgeService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;


        private readonly Lazy<IStateProvider> _lazyStateProvider;

        public HostSmartContractBridgeContext(ISmartContractBridgeService smartContractBridgeService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _smartContractBridgeService = smartContractBridgeService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;

            _lazyStateProvider = new Lazy<IStateProvider>(
                () => new CachedStateProvider(
                    new StateProvider {HostSmartContractBridgeContext = this}),
                LazyThreadSafetyMode.PublicationOnly);
        }


        public Transaction Transaction => TransactionContext.Transaction.Clone();

        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext SmartContractContext { get; set; }

        public IStateProvider StateProvider => _lazyStateProvider.Value;

        public Address GetContractAddressByName(Hash hash)
        {
            return _smartContractBridgeService.GetAddressByContractName(hash);
        }

        public void Initialize(ITransactionContext transactionContext,
            ISmartContractContext smartContractContext)
        {
            TransactionContext = transactionContext;
            SmartContractContext = smartContractContext;
        }

        public async Task<ByteString> GetStateAsync(string key)
        {
            return await _smartContractBridgeService.GetStateAsync(
                Self, key, CurrentHeight - 1, PreviousBlockHash);
        }

        public int ChainId => _smartContractBridgeService.GetChainId();

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

        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.Clone();
        public Address Self => SmartContractContext.ContractAddress.Clone();
        public Address Genesis => Address.Genesis;
        public long CurrentHeight => TransactionContext.BlockHeight;
        public DateTime CurrentBlockTime => TransactionContext.CurrentBlockTime;
        public Hash PreviousBlockHash => TransactionContext.PreviousBlockHash.Clone();

        public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            var cabBeRecovered = CryptoHelpers.RecoverPublicKey(signature, hash, out var publicKey);
            return !cabBeRecovered ? null : publicKey;
        }

        /// <summary>
        ///     Recovers the first public key signing this transaction.
        /// </summary>
        /// <returns>Public key byte array</returns>
        public byte[] RecoverPublicKey()
        {
            return RecoverPublicKey(TransactionContext.Transaction.Sigs.First().ToByteArray(),
                TransactionContext.Transaction.GetHash().DumpByteArray());
        }

        public T Call<T>(IStateCache stateCache, Address address, string methodName, ByteString args)
        {
            var trace = AsyncHelper.RunSync(async () =>
            {
                var chainContext = new ChainContext
                {
                    BlockHash = TransactionContext.PreviousBlockHash,
                    BlockHeight = TransactionContext.BlockHeight - 1,
                    StateCache = stateCache
                };

                var tx = new Transaction
                {
                    From = Self,
                    To = address,
                    MethodName = methodName,
                    Params = args
                };
                return await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, CurrentBlockTime);
            });

            if (!trace.IsSuccessful()) throw new ContractCallException(trace.StdErr);

            var decoder = ReturnTypeHelper.GetDecoder<T>();
            return decoder(trace.ReturnValue.ToByteArray());
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
                From = ConvertVirtualAddressToContractAddress(fromVirtualAddress),
                To = toAddress,
                MethodName = methodName,
                Params = args
            });
        }

        //TODO: review the method is safe, and can FromPublicKey accept a different length (may not 32) byte array?
        public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
        {
            return Address.FromPublicKey(Self.Value.Concat(
                virtualAddress.Value.ToByteArray().CalculateHash()).ToArray());
        }

        public Address GetZeroSmartContractAddress()
        {
            return _smartContractBridgeService.GetZeroSmartContractAddress();
        }


        public Block GetPreviousBlock()
        {
            return AsyncHelper.RunSync(() => _smartContractBridgeService.GetBlockByHashAsync(
                TransactionContext.PreviousBlockHash));
        }

        public bool VerifySignature(Transaction tx)
        {
            return tx.VerifySignature();
        }

        public void SendDeferredTransaction(Transaction deferredTxn)
        {
            TransactionContext.Trace.DeferredTransaction = deferredTxn.ToByteString();
        }

        public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
        {
            if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress()))
                throw new NoPermissionException();

            AsyncHelper.RunSync(() => _smartContractBridgeService.DeployContractAsync(address, registration,
                false, name));
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress()))
                throw new NoPermissionException();

            AsyncHelper.RunSync(() => _smartContractBridgeService.UpdateContractAsync(address, registration,
                false, null));
        }
    }
}