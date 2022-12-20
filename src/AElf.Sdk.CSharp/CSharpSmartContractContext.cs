using System;
using System.Collections.Generic;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Sdk.CSharp;

/// <summary>
///     Represents the transaction execution context in a smart contract. An instance of this class is present in the
///     base class for smart contracts (Context property). It provides access to properties and methods useful for
///     implementing the logic in smart contracts.
/// </summary>
public class CSharpSmartContractContext : ISmartContractBridgeContext
{
    public CSharpSmartContractContext(ISmartContractBridgeContext smartContractBridgeContextImplementation)
    {
        SmartContractBridgeContextImplementation = smartContractBridgeContextImplementation;
    }

    public ISmartContractBridgeContext SmartContractBridgeContextImplementation { get; }

    /// <summary>
    ///     Provides access to the underlying state provider.
    /// </summary>
    public IStateProvider StateProvider => SmartContractBridgeContextImplementation.StateProvider;

    /// <summary>
    ///     The chain id of the chain on which the contract is currently running.
    /// </summary>
    public int ChainId => SmartContractBridgeContextImplementation.ChainId;

    /// <summary>
    ///     Application logging - when writing a contract it is useful to be able to log some elements in the
    ///     applications log file to simplify development. Note that these logs are only visible when the node
    ///     executing the transaction is build in debug mode.
    /// </summary>
    /// <param name="func">The logic that will be executed for logging purposes.</param>
    public void LogDebug(Func<string> func)
    {
        SmartContractBridgeContextImplementation.LogDebug(func);
    }

    /// <summary>
    ///     This method is used to produce logs that can be found in the transaction result after execution.
    /// </summary>
    /// <param name="logEvent">The event to fire.</param>
    public void FireLogEvent(LogEvent logEvent)
    {
        SmartContractBridgeContextImplementation.FireLogEvent(logEvent);
    }

    /// <summary>
    ///     The ID of the transaction that's currently executing.
    /// </summary>
    public Hash TransactionId => SmartContractBridgeContextImplementation.TransactionId;

    /// <summary>
    ///     The Sender of the transaction that is executing.
    /// </summary>
    public Address Sender => SmartContractBridgeContextImplementation.Sender;

    /// <summary>
    ///     The address of the contract currently being executed. This changes for every transaction and inline transaction.
    /// </summary>
    public Address Self => SmartContractBridgeContextImplementation.Self;

    /// <summary>
    ///     The address of the sender (signer) of the transaction being executed. It’s type is an AElf address. It
    ///     corresponds to the From field of the transaction. This value never changes, even for nested inline calls.
    ///     This means that when you access this property in your contract, it’s value will be the entity that created
    ///     the transaction (user or smart contract through an inline call).
    /// </summary>
    public Address Origin => SmartContractBridgeContextImplementation.Origin;

    public Hash OriginTransactionId => SmartContractBridgeContextImplementation.OriginTransactionId;

    /// <summary>
    ///     The height of the block that contains the transaction currently executing.
    /// </summary>
    public long CurrentHeight => SmartContractBridgeContextImplementation.CurrentHeight;

    /// <summary>
    ///     The height of the block that contains the transaction before charging.
    /// </summary>
    public Transaction Transaction => SmartContractBridgeContextImplementation.Transaction;
    
    /// <summary>
    ///     The time included in the current blocks header.
    /// </summary>
    public Timestamp CurrentBlockTime => SmartContractBridgeContextImplementation.CurrentBlockTime;

    /// <summary>
    ///     The hash of the block that precedes the current in the blockchain structure.
    /// </summary>
    public Hash PreviousBlockHash => SmartContractBridgeContextImplementation.PreviousBlockHash;

    /// <summary>
    ///     Provides access to variable of the bridge.
    /// </summary>
    public ContextVariableDictionary Variables => SmartContractBridgeContextImplementation.Variables;

    /// <summary>
    ///     Recovers the public key of the transaction Sender.
    /// </summary>
    /// <returns>A byte array representing the public key.</returns>
    public byte[] RecoverPublicKey()
    {
        return SmartContractBridgeContextImplementation.RecoverPublicKey();
    }

    /// <summary>
    ///     Recovers the public key of the transaction Sender with custom args.
    /// </summary>
    /// <returns>A byte array representing the public key.</returns>
    public byte[] RecoverPublicKey(byte[] signature, byte[] hash)
    {
        return SmartContractBridgeContextImplementation.RecoverPublicKey(signature, hash);
    }

    /// <summary>
    ///     Returns the transaction included in the previous block (previous to the one currently executing).
    /// </summary>
    /// <returns>A list of transaction.</returns>
    public List<Transaction> GetPreviousBlockTransactions()
    {
        return SmartContractBridgeContextImplementation.GetPreviousBlockTransactions();
    }

    /// <summary>
    ///     Returns whether or not the given transaction is well formed and the signature is correct.
    /// </summary>
    /// <param name="tx">The transaction to verify.</param>
    /// <returns>The verification results.</returns>
    public bool VerifySignature(Transaction tx)
    {
        return SmartContractBridgeContextImplementation.VerifySignature(tx);
    }

    /// <summary>
    ///     Deploy a new smart contract (only the genesis contract can call it).
    /// </summary>
    /// <param name="address">The address of new smart contract.</param>
    /// <param name="registration">The registration of the new smart contract.</param>
    /// <param name="name">The hash value of the smart contract name.</param>
    public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
    {
        SmartContractBridgeContextImplementation.DeployContract(address, registration, name);
    }

    /// <summary>
    ///     Update a smart contract (only the genesis contract can call it).
    /// </summary>
    /// <param name="address">The address of smart contract to update.</param>
    /// <param name="registration">The registration of the smart contract to update.</param>
    /// <param name="name">The hash value of the smart contract name to update.</param>
    public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
    {
        SmartContractBridgeContextImplementation.UpdateContract(address, registration, name);
    }

    /// <summary>
    ///     Calls a method on another contract.
    /// </summary>
    /// <param name="fromAddress">The address to use as sender.</param>
    /// <param name="toAddress">The address of the contract you're seeking to interact with.</param>
    /// <param name="methodName">The name of method you want to call.</param>
    /// <param name="args">
    ///     The input arguments for calling that method. This is usually generated from the protobuf
    ///     definition of the input type
    /// </param>
    /// <typeparam name="T">The type of the return message.</typeparam>
    /// <returns>The result of the call.</returns>
    public T Call<T>(Address fromAddress, Address toAddress, string methodName, ByteString args)
        where T : IMessage<T>, new()
    {
        return SmartContractBridgeContextImplementation.Call<T>(fromAddress, toAddress, methodName, args);
    }

    /// <summary>
    ///     Sends an inline transaction to another contract.
    /// </summary>
    /// <param name="toAddress">The address of the contract you're seeking to interact with.</param>
    /// <param name="methodName">The name of method you want to invoke.</param>
    /// <param name="args">
    ///     The input arguments for calling that method. This is usually generated from the protobuf
    ///     definition of the input type.
    /// </param>
    public void SendInline(Address toAddress, string methodName, ByteString args)
    {
        SmartContractBridgeContextImplementation.SendInline(toAddress, methodName, args);
    }

    /// <summary>
    ///     Sends a virtual inline transaction to another contract.
    /// </summary>
    /// <param name="fromVirtualAddress">The virtual address to use as sender.</param>
    /// <param name="toAddress">The address of the contract you're seeking to interact with.</param>
    /// <param name="methodName">The name of method you want to invoke.</param>
    /// <param name="args">
    ///     The input arguments for calling that method. This is usually generated from the protobuf
    ///     definition of the input type.
    /// </param>
    public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
    {
        SmartContractBridgeContextImplementation.SendVirtualInline(fromVirtualAddress, toAddress, methodName,
            args);
    }

    /// <summary>
    ///     Sends a virtual inline transaction to another contract. This method is only available to system smart contract.
    /// </summary>
    /// <param name="fromVirtualAddress">The virtual address of the system contract to use as sender.</param>
    /// <param name="toAddress">The address of the contract you're seeking to interact with.</param>
    /// <param name="methodName">The name of method you want to invoke.</param>
    /// <param name="args">
    ///     The input arguments for calling that method. This is usually generated from the protobuf
    ///     definition of the input type.
    /// </param>
    public void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName,
        ByteString args)
    {
        SmartContractBridgeContextImplementation.SendVirtualInlineBySystemContract(fromVirtualAddress, toAddress,
            methodName, args);
    }

    /// <summary>
    ///     Converts a virtual address to a contract address with the contract address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address that want to convert.</param>
    /// <param name="contractAddress">The contract address.</param>
    /// <returns>The converted address.</returns>
    public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress, Address contractAddress)
    {
        return SmartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress,
            contractAddress);
    }

    /// <summary>
    ///     Converts a virtual address to a contract address with the contract hash name.
    /// </summary>
    /// <param name="virtualAddress">The virtual address that want to convert.</param>
    /// <param name="contractAddress">The contract address.</param>
    /// <returns></returns>
    public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress,
        Address contractAddress)
    {
        return SmartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddressWithContractHashName(
            virtualAddress, contractAddress);
    }

    /// <summary>
    ///     This method returns the address of the Genesis contract (smart contract zero) of the current chain.
    /// </summary>
    /// <returns>The address of the genesis contract.</returns>
    public Address GetZeroSmartContractAddress()
    {
        return SmartContractBridgeContextImplementation.GetZeroSmartContractAddress();
    }

    /// <summary>
    ///     This method returns the address of the Genesis contract (smart contract zero) of the specified chain.
    /// </summary>
    /// <param name="chainId">The chain's ID.</param>
    /// <returns>The address of the genesis contract, for the given chain.</returns>
    public Address GetZeroSmartContractAddress(int chainId)
    {
        return SmartContractBridgeContextImplementation.GetZeroSmartContractAddress(chainId);
    }

    /// <summary>
    ///     It's sometimes useful to get the address of a system contract. The input is a hash of the system contracts
    ///     name. These hashes are easily accessible through the constants in the SmartContractConstants.cs file of the
    ///     C# SDK.
    /// </summary>
    /// <param name="hash">The hash of the name.</param>
    /// <returns>The address of the system contract.</returns>
    public Address GetContractAddressByName(string hash)
    {
        return SmartContractBridgeContextImplementation.GetContractAddressByName(hash);
    }

    /// <summary>
    ///     Get the mapping that associates the system contract addresses and their name's hash.
    /// </summary>
    /// <returns>The addresses with their hashes.</returns>
    public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
    {
        return SmartContractBridgeContextImplementation.GetSystemContractNameToAddressMapping();
    }

    /// <summary>
    ///     Generate a hash type id based on the contract address and the bytes.
    /// </summary>
    /// <param name="contractAddress">The contract address on which the id generation is based.</param>
    /// <param name="bytes">The bytes on which the id generation is based.</param>
    /// <returns>The generated hash type id.</returns>
    public Hash GenerateId(Address contractAddress, IEnumerable<byte> bytes)
    {
        return SmartContractBridgeContextImplementation.GenerateId(contractAddress, bytes);
    }

    /// <summary>
    ///     Verify that the state size is within the valid value.
    /// </summary>
    /// <param name="obj">The state.</param>
    /// <returns>The state.</returns>
    /// <exception cref="T:AElf.Kernel.SmartContract.StateOverSizeException"> The state size exceeds the limit.</exception>
    public object ValidateStateSize(object obj)
    {
        return SmartContractBridgeContextImplementation.ValidateStateSize(obj);
    }

    /// <summary>
    ///     Gets a random hash based on the input hash.
    /// </summary>
    /// <param name="fromHash">Hash.</param>
    /// <returns>Random hash.</returns>
    public Hash GetRandomHash(Hash fromHash)
    {
        return SmartContractBridgeContextImplementation.GetRandomHash(fromHash);
    }

    /// <summary>
    ///     Converts the input hash to a 64-bit signed integer.
    /// </summary>
    /// <param name="hash">The hash.</param>
    /// <param name="start">The inclusive lower bound of the number returned.</param>
    /// <param name="end">
    ///     The exclusive upper bound of the number returned. endValue must be greater than or equal to
    ///     startValue.
    /// </param>
    /// <returns>The 64-bit signed integer.</returns>
    /// <exception cref="T:System.ArgumentException"> startValue is less than 0 or greater than endValue.</exception>
    public long ConvertHashToInt64(Hash hash, long start = 0, long end = long.MaxValue)
    {
        return SmartContractBridgeContextImplementation.ConvertHashToInt64(hash, start, end);
    }

    /// <summary>
    ///     Converts a virtual address to a contract address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address that want to convert.</param>
    /// <returns>The converted address.</returns>
    public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
    {
        return SmartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddress(virtualAddress);
    }

    /// <summary>
    ///     Converts a virtual address to a contract address with the current contract hash name.
    /// </summary>
    /// <param name="virtualAddress">The virtual address that want to convert.</param>
    /// <returns>The converted address.</returns>
    public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress)
    {
        return SmartContractBridgeContextImplementation.ConvertVirtualAddressToContractAddressWithContractHashName(
            virtualAddress);
    }
}