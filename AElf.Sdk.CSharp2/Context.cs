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
        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext SmartContractContext { get; set; }

        public void FireEvent(Event logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent.GetLogEvent(Self));
        }

        public Hash TransactionId => TransactionContext.Transaction.GetHash();
        public Address Sender => TransactionContext.Transaction.From.ToReadOnly();
        public Address Self => SmartContractContext.ContractAddress.ToReadOnly();

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

        private static byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            var cabBeRecovered = CryptoHelpers.RecoverPublicKey(signature, hash, out var publicKey);
            return !cabBeRecovered ? null : publicKey;
        }
    }
}