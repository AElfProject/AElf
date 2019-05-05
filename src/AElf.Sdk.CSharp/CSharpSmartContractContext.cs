using System;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class CSharpSmartContractContext
    {
        private readonly ISmartContractBridgeContext _smartContractBridgeContextImplementation;

        public ISmartContractBridgeContext SmartContractBridgeContextImplementation =>
            _smartContractBridgeContextImplementation;

        public CSharpSmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation)
        {
            _smartContractBridgeContextImplementation = smartContractBridgeContextImplementation;
        }

        public IStateProvider StateProvider => _smartContractBridgeContextImplementation.StateProvider;
        public int ChainId => _smartContractBridgeContextImplementation.ChainId;

        public void LogDebug(Func<string> func)
        {
            _smartContractBridgeContextImplementation.LogDebug(func);
        }

        public void FireLogEvent(LogEvent logEvent)
        {
            _smartContractBridgeContextImplementation.FireLogEvent(logEvent);
        }

        public Hash TransactionId => _smartContractBridgeContextImplementation.TransactionId;

        public Address Sender => _smartContractBridgeContextImplementation.Sender;

        public Address Self => _smartContractBridgeContextImplementation.Self;

        public Address Genesis => _smartContractBridgeContextImplementation.Genesis;

        public long CurrentHeight => _smartContractBridgeContextImplementation.CurrentHeight;

        public DateTime CurrentBlockTime => _smartContractBridgeContextImplementation.CurrentBlockTime;

        public Hash PreviousBlockHash => _smartContractBridgeContextImplementation.PreviousBlockHash;

        public ContextVariableDictionary Variables => _smartContractBridgeContextImplementation.Variables;
        
        public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            return _smartContractBridgeContextImplementation.RecoverPublicKey(signature, hash);
        }

        public byte[] RecoverPublicKey()
        {
            return _smartContractBridgeContextImplementation.RecoverPublicKey();
        }

        public IBlockBase GetPreviousBlock()
        {
            return _smartContractBridgeContextImplementation.GetPreviousBlock();
        }

        public bool VerifySignature(Transaction tx)
        {
            return _smartContractBridgeContextImplementation.VerifySignature(tx);
        }

        public void SendDeferredTransaction(Transaction deferredTxn)
        {
            _smartContractBridgeContextImplementation.SendDeferredTransaction(deferredTxn);
        }

        public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
        {
            _smartContractBridgeContextImplementation.DeployContract(address, registration, name);
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            _smartContractBridgeContextImplementation.UpdateContract(address, registration, name);
        }

        public T Call<T>(Address address, string methodName, ByteString args)
            where T : IMessage<T>, new()
        {
            return _smartContractBridgeContextImplementation.Call<T>(address, methodName, args);
        }

        public void SendInline(Address toAddress, string methodName, ByteString args)
        {
            _smartContractBridgeContextImplementation.SendInline(toAddress, methodName, args);
        }

        public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
        {
            _smartContractBridgeContextImplementation.SendVirtualInline(fromVirtualAddress, toAddress, methodName, args);
        }

        public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
        {
            return _smartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress);
        }

        public Address GetZeroSmartContractAddress()
        {
            return _smartContractBridgeContextImplementation.GetZeroSmartContractAddress();
        }
        
        public byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return _smartContractBridgeContextImplementation.EncryptMessage(receiverPublicKey, plainMessage);
        }

        public byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return _smartContractBridgeContextImplementation.DecryptMessage(senderPublicKey, cipherMessage);
        }
    }
}