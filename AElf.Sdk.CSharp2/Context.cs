using System;
using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Cryptography;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Types;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.Kernel.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Sdk.CSharp
{
    public class Context : IContextInternal
    {
        private IBlockchainService _blockChain { get; set; }
        private ISmartContractContext _smartContractContext;
        public ITransactionContext TransactionContext { get; set; }

        public ISmartContractContext SmartContractContext
        {
            get => _smartContractContext;
            set
            {
                _smartContractContext = value;
                OnSmartContractContextSet();
            }
        }

        private void OnSmartContractContextSet()
        {
            _blockChain = _smartContractContext.ChainService;
        }

        public void FireEvent(Event logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent.GetLogEvent(Self));
        }

        public int ChainId => SmartContractContext.ChainId;
        public Transaction Transaction => TransactionContext.Transaction.ToReadOnly();
        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.ToReadOnly();
        public Address Self => SmartContractContext.ContractAddress.ToReadOnly();
        public Address Genesis => Address.Genesis.ToReadOnly();
        public ulong CurrentHeight => TransactionContext.BlockHeight;
        public DateTime CurrentBlockTime => TransactionContext.CurrentBlockTime;
        public Hash PreviousBlockHash => TransactionContext.PreviousBlockHash.ToReadOnly();

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

        public void SendInline(Address address, string methodName, params object[] args)
        {
            TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = TransactionContext.Transaction.From,
                To = address,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }


        public Block GetPreviousBlock()
        {
            return AsyncHelper.RunSync(
                () => _blockChain.GetBlockByHashAsync(_smartContractContext.ChainId,
                    TransactionContext.PreviousBlockHash));
        }

        public bool VerifySignature(Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1 && tx.Type != TransactionType.MsigTransaction)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(),
                    tx.GetHash().DumpByteArray(), out var pubKey);
                return canBeRecovered && Address.FromPublicKey(pubKey).Equals(tx.From);
            }

            return true;
        }

        public void SendDeferredTransaction(Transaction deferredTxn)
        {
            TransactionContext.Trace.DeferredTransaction = deferredTxn.ToByteString();
        }

        public void DeployContract(Address address, SmartContractRegistration registration)
        {
            if (!Self.Equals(ContractHelpers.GetGenesisBasicContractAddress(ChainId)))
            {
                throw new AssertionError("no permission.");
            }

            AsyncHelper.RunSync(async () =>
                await SmartContractContext.SmartContractService.DeployContractAsync(ChainId, address, registration,
                    false));
        }

        public void UpdateContract(Address address, SmartContractRegistration registration)
        {
            if (!Self.Equals(ContractHelpers.GetGenesisBasicContractAddress(ChainId)))
            {
                throw new AssertionError("no permission.");
            }

            AsyncHelper.RunSync(async () =>
                await SmartContractContext.SmartContractService.UpdateContractAsync(ChainId, address, registration,
                    false));
        }
    }
}