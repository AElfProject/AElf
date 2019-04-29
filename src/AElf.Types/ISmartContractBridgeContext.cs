using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf
{
    public class ContextVariableDictionary : ReadOnlyDictionary<string, string>
    {
        public ContextVariableDictionary(IDictionary<string, string> dictionary) : base(dictionary)
        {
        
        }

        public string NativeSymbol => this[nameof(NativeSymbol)];

        public const string NativeSymbolName = nameof(NativeSymbol);
    
        public string TimeEachTerm => this[nameof(TimeEachTerm)];

        public const string TimeEachTermName = nameof(TimeEachTerm);

        public string MinimumLockTime => this[nameof(MinimumLockTime)];
    
        public const string MinimumLockTimeName = nameof(MinimumLockTime);

        public string MaximumLockTime => this[nameof(MaximumLockTime)];
    
        public const string MaximumLockTimeName = nameof(MaximumLockTime);

        public string BaseTimeUnit => this[nameof(BaseTimeUnit)];

        public const string BaseTimeUnitName = nameof(BaseTimeUnit);
    }

    public interface ISmartContractBridgeContext
    {
        int ChainId { get; }

        ContextVariableDictionary Variables { get; }

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
        IBlockBase GetPreviousBlock();

        bool VerifySignature(Transaction tx);

        /// <summary>
        /// Generate txn not executed before next block. 
        /// </summary>
        /// <param name="deferredTxn"></param>
        void SendDeferredTransaction(Transaction deferredTxn);

        void DeployContract(Address address, SmartContractRegistration registration, Hash name);

        void UpdateContract(Address address, SmartContractRegistration registration, Hash name);

        T Call<T>(Address address, string methodName, ByteString args) where T : IMessage<T>, new();
        void SendInline(Address toAddress, string methodName, ByteString args);

        void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args);

        Address ConvertVirtualAddressToContractAddress(Hash virtualAddress);

        Address GetZeroSmartContractAddress();

        IStateProvider StateProvider { get; }

        byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage);

        byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage);
    }
}