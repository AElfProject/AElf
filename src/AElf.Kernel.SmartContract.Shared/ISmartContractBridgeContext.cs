using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract;

/// <summary>
///     The transaction execution context in the smart contract.
/// </summary>
public interface ISmartContractBridgeContext
{
    int ChainId { get; }

    ContextVariableDictionary Variables { get; }

    Hash TransactionId { get; }

    Address Sender { get; }

    Address Self { get; }

    Address Origin { get; }

    Hash OriginTransactionId { get; }

    long CurrentHeight { get; }
    
    Transaction Transaction { get; }

    Timestamp CurrentBlockTime { get; }
    Hash PreviousBlockHash { get; }

    IStateProvider StateProvider { get; }

    void LogDebug(Func<string> func);

    void FireLogEvent(LogEvent logEvent);

    byte[] RecoverPublicKey();

    byte[] RecoverPublicKey(byte[] signature, byte[] hash);

    List<Transaction> GetPreviousBlockTransactions();

    bool VerifySignature(Transaction tx);

    void DeployContract(Address address, SmartContractRegistration registration, Hash name);
    
    void UpdateContract(Address address, SmartContractRegistration registration, Hash name);
    
    ContractInfoDto DeploySmartContract(Address address, SmartContractRegistration registration,Hash name);

    ContractInfoDto UpdateSmartContract(Address address, SmartContractRegistration registration, Hash name, string previousContractVersion);

    ContractVersionCheckDto CheckContractVersion(string previousContractVersion, SmartContractRegistration registration);

    T Call<T>(Address fromAddress, Address toAddress, string methodName, ByteString args)
        where T : IMessage<T>, new();

    void SendInline(Address toAddress, string methodName, ByteString args);

    void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args);

    void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args,
        bool logTransaction);

    void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName,
        ByteString args);

    void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName,
        ByteString args, bool logTransaction);

    Address ConvertVirtualAddressToContractAddress(Hash virtualAddress, Address contractAddress);

    Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress,
        Address contractAddress);

    Address GetZeroSmartContractAddress();

    Address GetZeroSmartContractAddress(int chainId);

    Address GetContractAddressByName(string hash);

    IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping();

    Hash GenerateId(Address contractAddress, IEnumerable<byte> bytes);

    Hash GetRandomHash(Hash fromHash);

    long ConvertHashToInt64(Hash hash, long start = 0, long end = long.MaxValue);

    object ValidateStateSize(object obj);

    bool ECVrfVerify(byte[] pubKey, byte[] alpha, byte[] pi, out byte[] beta);
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

[Serializable]
public class StateOverSizeException : SmartContractBridgeException
{
    public StateOverSizeException()
    {
    }

    public StateOverSizeException(string message) : base(message)
    {
    }

    public StateOverSizeException(string message, Exception inner) : base(message, inner)
    {
    }

    protected StateOverSizeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

[Serializable]
public class StateKeyOverSizeException : SmartContractBridgeException
{
    public StateKeyOverSizeException()
    {
    }

    public StateKeyOverSizeException(string message) : base(message)
    {
    }

    public StateKeyOverSizeException(string message, Exception inner) : base(message, inner)
    {
    }

    protected StateKeyOverSizeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}