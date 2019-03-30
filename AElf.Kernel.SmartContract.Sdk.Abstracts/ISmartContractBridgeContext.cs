using System;
using System.Runtime.Serialization;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Sdk
{
    //TODO: this assembly should not reference AElf.Kernel.Types,
    //BODY: because it may be changed very often, and may introduce new Type, if some DAPP user use it,
    //it will be very hard to remove the type in the assembly.
    //we should define a new assembly, it only contains types for smart contract.
    public interface ISmartContractBridgeContext
    {
        int ChainId { get; }

        void LogDebug(Func<string> func);

        void FireLogEvent(LogEvent logEvent);

        Hash TransactionId { get; }

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

        T Call<T>(IStateCache stateCache, Address address, string methodName, ByteString args)
            where T : IMessage<T>, new();
        
        void SendInline(Address toAddress, string methodName, ByteString args);

        void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args);

        Address ConvertVirtualAddressToContractAddress(Hash virtualAddress);

        Address GetZeroSmartContractAddress();

        IStateProvider StateProvider { get; }

        byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage);

        byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage);
    }

    public interface ILimitedSmartContractContext
    {
        
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
    public class NoPermissionException : SmartContractBridgeException
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


    [Serializable]
    public class ContractCallException : SmartContractBridgeException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ContractCallException()
        {
        }

        public ContractCallException(string message) : base(message)
        {
        }

        public ContractCallException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ContractCallException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}