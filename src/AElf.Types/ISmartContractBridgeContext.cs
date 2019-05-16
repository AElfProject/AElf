using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Types;
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
        
        long CurrentHeight { get; }

        DateTime CurrentBlockTime { get; }
        Hash PreviousBlockHash { get; }

        byte[] RecoverPublicKey();

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