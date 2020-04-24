using System;
using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel.SmartContract;


namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// represents the transaction execution context in a smart contract. An instance of this class is present in the
    /// base class for smart contracts (Context property). It provides access to properties and methods useful for
    /// implementing the logic in smart contracts.
    /// </summary>
    public class CSharpSmartContractContext : ISmartContractBridgeContext
    {
        private readonly ISmartContractBridgeContext _smartContractBridgeContextImplementation;

        public ISmartContractBridgeContext SmartContractBridgeContextImplementation =>
            _smartContractBridgeContextImplementation;

        public CSharpSmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation)
        {
            _smartContractBridgeContextImplementation = smartContractBridgeContextImplementation;
        }

        /// <summary>
        /// Provides access to the underlying state provider.
        /// </summary>
        public IStateProvider StateProvider => _smartContractBridgeContextImplementation.StateProvider;

        /// <summary>
        /// The chain id of the chain on which the contract is currently running.
        /// </summary>
        public int ChainId => _smartContractBridgeContextImplementation.ChainId;

        /// <summary>
        /// Application logging - when writing a contract it is useful to be able to log some elements in the
        /// applications log file to simplify development. Note that these logs are only visible when the node
        /// executing the transaction is build in debug mode.
        /// </summary>
        /// <param name="func">the logic that will be executed for logging purposes.</param>
        public void LogDebug(Func<string> func)
        {
            _smartContractBridgeContextImplementation.LogDebug(func);
        }

        /// <summary>
        /// This method is used to produce logs that can be found in the transaction result after execution.
        /// </summary>
        /// <param name="logEvent">The event to fire.</param>
        public void FireLogEvent(LogEvent logEvent)
        {
            _smartContractBridgeContextImplementation.FireLogEvent(logEvent);
        }

        /// <summary>
        /// The ID of the transaction that's currently executing.
        /// </summary>
        public Hash TransactionId => _smartContractBridgeContextImplementation.TransactionId;

        /// <summary>
        /// The Sender of the transaction that is executing.
        /// </summary>
        public Address Sender => _smartContractBridgeContextImplementation.Sender;

        /// <summary>
        /// The address of the contract currently being executed. This changes for every transaction and inline transaction.
        /// </summary>
        public Address Self => _smartContractBridgeContextImplementation.Self;

        /// <summary>
        /// The address of the sender (signer) of the transaction being executed. It’s type is an AElf address. It
        /// corresponds to the From field of the transaction. This value never changes, even for nested inline calls.
        /// This means that when you access this property in your contract, it’s value will be the entity that created
        /// the transaction (user or smart contract through an inline call).
        /// </summary>
        public Address Origin => _smartContractBridgeContextImplementation.Origin;

        public Hash OriginTransactionId => _smartContractBridgeContextImplementation.OriginTransactionId;

        /// <summary>
        /// The height of the block that contains the transaction currently executing.
        /// </summary>
        public long CurrentHeight => _smartContractBridgeContextImplementation.CurrentHeight;

        /// <summary>
        /// The time included in the current blocks header.
        /// </summary>
        public Timestamp CurrentBlockTime => _smartContractBridgeContextImplementation.CurrentBlockTime;

        /// <summary>
        /// The hash of the block that precedes the current in the blockchain structure.
        /// </summary>
        public Hash PreviousBlockHash => _smartContractBridgeContextImplementation.PreviousBlockHash;

        /// <summary>
        /// Provides access to variable of the bridge.
        /// </summary>
        public ContextVariableDictionary Variables => _smartContractBridgeContextImplementation.Variables;

        /// <summary>
        /// Recovers the public key of the transaction Sender.
        /// </summary>
        /// <returns>A byte array representing the public key.</returns>
        public byte[] RecoverPublicKey()
        {
            return _smartContractBridgeContextImplementation.RecoverPublicKey();
        }

        /// <summary>
        /// Returns the transaction included in the previous block (previous to the one currently executing).
        /// </summary>
        /// <returns>A list of transaction.</returns>
        public List<Transaction> GetPreviousBlockTransactions()
        {
            return _smartContractBridgeContextImplementation.GetPreviousBlockTransactions();
        }

        /// <summary>
        /// Returns whether or not the given transaction is well formed and the signature is correct.
        /// </summary>
        /// <param name="tx">the transaction to verify</param>
        /// <returns>true if correct, false otherwise</returns>
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

        /// <summary>
        /// Calls a method on another contract.
        /// </summary>
        /// <param name="address">the address of the contract you're seeking to interact with</param>
        /// <param name="methodName">the name of method you want to call</param>
        /// <param name="args">the input arguments for calling that method. This is usually generated from the protobuf
        /// definition of the input type</param>
        /// <typeparam name="T">The type of the return message.</typeparam>
        /// <returns>The result of the call.</returns>
        public T Call<T>(Address fromAddress, Address toAddress, string methodName, ByteString args)
            where T : IMessage<T>, new()
        {
            return _smartContractBridgeContextImplementation.Call<T>(fromAddress, toAddress, methodName, args);
        }

        /// <summary>
        /// Sends an inline transaction to another contract.
        /// </summary>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="args">the input arguments for calling that method. This is usually generated from the protobuf
        /// definition of the input type.</param>
        public void SendInline(Address toAddress, string methodName, ByteString args)
        {
            _smartContractBridgeContextImplementation.SendInline(toAddress, methodName, args);
        }

        /// <summary>
        /// Sends a virtual inline transaction to another contract.
        /// </summary>
        /// <param name="fromVirtualAddress">the virtual address to use as sender.</param>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="args">the input arguments for calling that method. This is usually generated from the protobuf
        /// definition of the input type.</param>
        public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
        {
            _smartContractBridgeContextImplementation.SendVirtualInline(fromVirtualAddress, toAddress, methodName,
                args);
        }

        /// <summary>
        /// Like SendVirtualInline but the virtual address us a system smart contract. 
        /// </summary>
        /// <param name="fromVirtualAddress">the virtual address of the system contract to use as sender.</param>
        /// <param name="toAddress">the address of the contract you're seeking to interact with.</param>
        /// <param name="methodName">the name of method you want to invoke.</param>
        /// <param name="args">the input arguments for calling that method. This is usually generated from the protobuf
        /// definition of the input type.</param>
        public void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName,
            ByteString args)
        {
            _smartContractBridgeContextImplementation.SendVirtualInlineBySystemContract(fromVirtualAddress, toAddress,
                methodName, args);
        }

        /// <summary>
        /// Converts a virtual address to the contracts address.
        /// </summary>
        /// <param name="virtualAddress">The address.</param>
        /// <returns>The converted address.</returns>
        public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
        {
            return _smartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress);
        }

        /// <summary>
        /// Converts a virtual address to the contracts address.
        /// </summary>
        /// <param name="virtualAddress"></param>
        /// <returns></returns>
        public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress)
        {
            return _smartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddressWithContractHashName(
                virtualAddress);
        }

        public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress, Address contractAddress)
        {
            return _smartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress,
                contractAddress);
        }

        public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress,
            Address contractAddress)
        {
            return _smartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddressWithContractHashName(
                virtualAddress, contractAddress);
        }

        /// <summary>
        /// This method returns the address of the Genesis contract (smart contract zero) of the current chain.
        /// </summary>
        /// <returns>The address of the genesis contract.</returns>
        public Address GetZeroSmartContractAddress()
        {
            return _smartContractBridgeContextImplementation.GetZeroSmartContractAddress();
        }

        /// <summary>
        /// This method returns the address of the Genesis contract (smart contract zero) of the specified chain.
        /// </summary>
        /// <param name="chainId">The chain's ID.</param>
        /// <returns>The address of the genesis contract, for the given chain.</returns>
        public Address GetZeroSmartContractAddress(int chainId)
        {
            return _smartContractBridgeContextImplementation.GetZeroSmartContractAddress(chainId);
        }

        /// <summary>
        /// It's sometimes useful to get the address of a system contract. The input is a hash of the system contracts
        /// name. These hashes are easily accessible through the constants in the SmartContractConstants.cs file of the
        /// C# SDK.
        /// </summary>
        /// <param name="hash">The hash of the name.</param>
        /// <returns>The address of the system contract.</returns>
        public Address GetContractAddressByName(string hash)
        {
            return _smartContractBridgeContextImplementation.GetContractAddressByName(hash);
        }

        /// <summary>
        /// Get the mapping that associates the system contract addresses and their name's hash.
        /// </summary>
        /// <returns>The addresses with their hashes.</returns>
        public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
        {
            return _smartContractBridgeContextImplementation.GetSystemContractNameToAddressMapping();
        }

        /// <summary>
        /// Encrypts a message with the given public key.
        /// </summary>
        /// <param name="receiverPublicKey">The receivers public key.</param>
        /// <param name="plainMessage">The non encrypted message.</param>
        /// <returns>The encrypted message.</returns>
        public byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return _smartContractBridgeContextImplementation.EncryptMessage(receiverPublicKey, plainMessage);
        }

        /// <summary>
        /// Decrypts a message with the given public key.
        /// </summary>
        /// <param name="senderPublicKey">The public key that encrypted the message.</param>
        /// <param name="cipherMessage">The encrypted message.</param>
        /// <returns>The decrypted message.</returns>
        public byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return _smartContractBridgeContextImplementation.DecryptMessage(senderPublicKey, cipherMessage);
        }

        public Hash GenerateId(Address contractAddress, IEnumerable<byte> bytes)
        {
            return _smartContractBridgeContextImplementation.GenerateId(contractAddress, bytes);
        }
    }
}