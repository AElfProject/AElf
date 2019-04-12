using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
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
        private readonly IAccountService _accountService;

        public HostSmartContractBridgeContext(ISmartContractBridgeService smartContractBridgeService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, IAccountService accountService)
        {
            _smartContractBridgeService = smartContractBridgeService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _accountService = accountService;
            var self = this;
            Address GetAddress() => self.Transaction.To;
            _lazyStateProvider = new Lazy<IStateProvider>(
                () => new CachedStateProvider(
                    new ScopedStateProvider()
                    {
                        ContractAddress = GetAddress(),
                        HostSmartContractBridgeContext = this
                    }),
                LazyThreadSafetyMode.PublicationOnly);
        }

        private ITransactionContext _transactionContext;

        public ITransactionContext TransactionContext
        {
            get => _transactionContext;
            set
            {
                _transactionContext = value;
                StateProvider.Cache = _transactionContext?.StateCache ?? new NullStateCache();
            }
        }

        private readonly Lazy<IStateProvider> _lazyStateProvider;

        public IStateProvider StateProvider => _lazyStateProvider.Value;
        public Address GetContractAddressByName(Hash hash)
        {
            return _smartContractBridgeService.GetAddressByContractName(hash);
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

        public byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return AsyncHelper.RunSync(() => _accountService.EncryptMessage(receiverPublicKey, plainMessage));
        }

        public byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return AsyncHelper.RunSync(() => _accountService.DecryptMessage(senderPublicKey, cipherMessage));
        }

        public Transaction Transaction => TransactionContext.Transaction.Clone();
        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.Clone();
        public Address Self => TransactionContext.Transaction.To.Clone();
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
        /// Recovers the first public key signing this transaction.
        /// </summary>
        /// <returns>Public key byte array</returns>
        public byte[] RecoverPublicKey()
        {
            return RecoverPublicKey(TransactionContext.Transaction.Sigs.First().ToByteArray(),
                TransactionContext.Transaction.GetHash().DumpByteArray());
        }

        public T Call<T>(IStateCache stateCache, Address address, string methodName, ByteString args)
            where T : IMessage<T>, new()
        {
            TransactionTrace trace = AsyncHelper.RunSync(async () =>
            {
                var chainContext = new ChainContext()
                {
                    BlockHash = this.TransactionContext.PreviousBlockHash,
                    BlockHeight = this.TransactionContext.BlockHeight - 1,
                    StateCache = stateCache
                };

                var tx = new Transaction()
                {
                    From = this.Self,
                    To = address,
                    MethodName = methodName,
                    Params = args
                };
                return await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, CurrentBlockTime);
            });

            if (!trace.IsSuccessful())
            {
                throw new ContractCallException(trace.StdErr);
            }

            var obj = new T();
            obj.MergeFrom(trace.ReturnValue);
            return obj;
        }

        public void SendInline(Address toAddress, string methodName, ByteString args)
        {
            TransactionContext.Trace.InlineTransactions.Add(new Transaction()
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
            TransactionContext.Trace.InlineTransactions.Add(new Transaction()
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
            {
                throw new NoPermissionException();
            }

            AsyncHelper.RunSync(() => _smartContractBridgeService.DeployContractAsync(address, registration,
                false, name));
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            if (!Self.Equals(_smartContractBridgeService.GetZeroSmartContractAddress()))
            {
                throw new NoPermissionException();
            }

            AsyncHelper.RunSync(() => _smartContractBridgeService.UpdateContractAsync(address, registration,
                false, null));
        }
    }
}