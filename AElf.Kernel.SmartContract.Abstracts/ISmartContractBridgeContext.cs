using System;
using System.Runtime.Serialization;
using AElf.Common;

namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractBridgeContext
    {
        int ChainId { get; }

        void LogDebug(Func<string> func);

        void FireLogEvent(LogEvent logEvent);

        Hash TransactionId { get; }

        // TODO: Remove Transaction
        Transaction Transaction { get; }
        Address Sender { get; }

        Address Self { get; }

        // TODO: Remove genesis
        Address Genesis { get; }
        long CurrentHeight { get; }

        DateTime CurrentBlockTime { get; }
        Hash PreviousBlockHash { get; }

        // TODO: Remove RecoverPublicKey(byte[] signature, byte[] hash)
        byte[] RecoverPublicKey(byte[] signature, byte[] hash);

        byte[] RecoverPublicKey();

        // TODO: Remove GetBlockByHeight
        Block GetPreviousBlock();

        bool VerifySignature(Transaction tx);

        /// <summary>
        /// Generate txn not executed before next block. 
        /// </summary>
        /// <param name="deferredTxn"></param>
        void SendDeferredTransaction(Transaction deferredTxn);

        void DeployContract(Address address, SmartContractRegistration registration, Hash name);

        void UpdateContract(Address address, SmartContractRegistration registration, Hash name);

        void SendInline(Address address, string methodName, params object[] args);
        T Call<T>(IStateCache stateCache, Address address, string methodName, params object[] args);
    }

    [Serializable]
    public class SmartContractBridgeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SmartContractBridgeException()
        {
        }

        public SmartContractBridgeException(string message) : base(message)
        {
        }

        public SmartContractBridgeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SmartContractBridgeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class NoPermissionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public NoPermissionException()
        {
        }

        public NoPermissionException(string message) : base(message)
        {
        }

        public NoPermissionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NoPermissionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    
}