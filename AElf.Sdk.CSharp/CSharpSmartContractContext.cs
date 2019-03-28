using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class CSharpSmartContractContext
    {
        public CSharpSmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation)
        {
            SmartContractBridgeContextImplementation = smartContractBridgeContextImplementation;
        }

        public ISmartContractBridgeContext SmartContractBridgeContextImplementation { get; }

        public int ChainId => SmartContractBridgeContextImplementation.ChainId;

        public Hash TransactionId => SmartContractBridgeContextImplementation.TransactionId;

        public Address Sender => SmartContractBridgeContextImplementation.Sender;

        public Address Self => SmartContractBridgeContextImplementation.Self;

        public Address Genesis => SmartContractBridgeContextImplementation.Genesis;

        public long CurrentHeight => SmartContractBridgeContextImplementation.CurrentHeight;

        public DateTime CurrentBlockTime => SmartContractBridgeContextImplementation.CurrentBlockTime;

        public Hash PreviousBlockHash => SmartContractBridgeContextImplementation.PreviousBlockHash;

        public void LogDebug(Func<string> func)
        {
            SmartContractBridgeContextImplementation.LogDebug(func);
        }

        public void FireLogEvent(LogEvent logEvent)
        {
            SmartContractBridgeContextImplementation.FireLogEvent(logEvent);
        }

        public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            return SmartContractBridgeContextImplementation.RecoverPublicKey(signature, hash);
        }

        public byte[] RecoverPublicKey()
        {
            return SmartContractBridgeContextImplementation.RecoverPublicKey();
        }

        public Block GetPreviousBlock()
        {
            return SmartContractBridgeContextImplementation.GetPreviousBlock();
        }

        public bool VerifySignature(Transaction tx)
        {
            return SmartContractBridgeContextImplementation.VerifySignature(tx);
        }

        public void SendDeferredTransaction(Transaction deferredTxn)
        {
            SmartContractBridgeContextImplementation.SendDeferredTransaction(deferredTxn);
        }

        public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
        {
            SmartContractBridgeContextImplementation.DeployContract(address, registration, name);
        }

        public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
        {
            SmartContractBridgeContextImplementation.UpdateContract(address, registration, name);
        }

        public T Call<T>(IStateCache stateCache, Address address, string methodName, ByteString args)
        {
            return SmartContractBridgeContextImplementation.Call<T>(stateCache, address, methodName, args);
        }

        public void SendInline(Address toAddress, string methodName, ByteString args)
        {
            SmartContractBridgeContextImplementation.SendInline(toAddress, methodName, args);
        }

        public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
        {
            SmartContractBridgeContextImplementation.SendVirtualInline(fromVirtualAddress, toAddress, methodName, args);
        }

        public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
        {
            return SmartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress);
        }

        public Address GetZeroSmartContractAddress()
        {
            return SmartContractBridgeContextImplementation.GetZeroSmartContractAddress();
        }
    }
}