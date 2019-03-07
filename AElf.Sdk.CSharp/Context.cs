using System;
using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Cryptography;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.Kernel.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class Context : IContextInternal
    {
        public ITransactionContext TransactionContext { get; set; }

        public ISmartContractContext SmartContractContext { get; set; }

        public int ChainId => SmartContractContext.GetChainId();

        public void LogDebug(Func<string> func)
        {
#if DEBUG
            SmartContractContext.LogDebug(func);
#endif
        }

        public void FireEvent<TEvent>(TEvent e) where TEvent : Event
        {
            var logEvent = EventParser<TEvent>.ToLogEvent(e, Self);
            TransactionContext.Trace.Logs.Add(logEvent);
        }

        public Transaction Transaction => TransactionContext.Transaction.ToReadOnly();
        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.ToReadOnly();
        public Address Self => SmartContractContext.ContractAddress.ToReadOnly();
        public Address Genesis => Address.Genesis;
        public long CurrentHeight => TransactionContext.BlockHeight;
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
                From = Self,
                To = address,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }


        public Block GetPreviousBlock()
        {
            return SmartContractContext.GetBlockByHash(
                TransactionContext.PreviousBlockHash);
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

        public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
        {
            //TODO: only check it in sdk not safe, we should check the security in the implement, in the 
            //method SmartContractContext.DeployContract or it's service 
            if (!Self.Equals(SmartContractContext.GetZeroSmartContractAddress()))
            {
                throw new AssertionError("no permission.");
            }

            SmartContractContext.DeployContract(address, registration,
                false, name);
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            if (!Self.Equals(SmartContractContext.GetZeroSmartContractAddress()))
            {
                throw new AssertionError("no permission.");
            }

            SmartContractContext.UpdateContract(address, registration,
                false, null);
        }
    }
}