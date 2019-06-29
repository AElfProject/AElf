using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp
{
    public class CSharpSmartContractContext : ISmartContractBridgeContext
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
        public Address Origin => _smartContractBridgeContextImplementation.Origin;

        public long CurrentHeight => _smartContractBridgeContextImplementation.CurrentHeight;

        public Timestamp CurrentBlockTime => _smartContractBridgeContextImplementation.CurrentBlockTime;

        public Hash PreviousBlockHash => _smartContractBridgeContextImplementation.PreviousBlockHash;

        public ContextVariableDictionary Variables => _smartContractBridgeContextImplementation.Variables;

        public byte[] RecoverPublicKey()
        {
            return _smartContractBridgeContextImplementation.RecoverPublicKey();
        }

        public List<Transaction> GetPreviousBlockTransactions()
        {
            return _smartContractBridgeContextImplementation.GetPreviousBlockTransactions();
        }

        public bool VerifySignature(Transaction tx)
        {
            return _smartContractBridgeContextImplementation.VerifySignature(tx);
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

        public Address GetContractAddressByName(Hash hash)
        {
            return _smartContractBridgeContextImplementation.GetContractAddressByName(hash);
        }
        
        public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
        {
            return _smartContractBridgeContextImplementation.GetSystemContractNameToAddressMapping();
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