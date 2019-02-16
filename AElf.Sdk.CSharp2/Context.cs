using System;
using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Cryptography;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.Sdk.CSharp.State;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class Context : IContextInternal
    {
        private IBlockChain BlockChain { get; set; }
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
            BlockChain = _smartContractContext.ChainService.GetBlockChain(_smartContractContext.ChainId);
        }

        public void FireEvent(Event logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent.GetLogEvent(Self));
        }

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

        public Block GetBlockByHeight(ulong height)
        {
            return (Block) BlockChain.GetBlockByHeightAsync(height, true).Result;
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
    }
}