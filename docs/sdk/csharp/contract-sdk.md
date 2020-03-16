# C# contract SDK 

## todo intro

## CSharpSmartContractContext

### Properties

#### StateProvider
#### ChainId

#### TransactionId
#### Sender
Self
Origin
CurrentHeight
CurrentBlockTime
PreviousBlockHash
Variables

### Methods

#### LogDebug
```csharp
public void LogDebug(Func<string> func)
```

public void FireLogEvent(LogEvent logEvent)
public byte[] RecoverPublicKey()
public List<Transaction> GetPreviousBlockTransactions()
public bool VerifySignature(Transaction tx)
public void DeployContract(Address address, SmartContractRegistration registration, Hash name)
public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
public T Call<T>(Address address, string methodName, ByteString args) where T : IMessage<T>, new()
public void SendInline(Address toAddress, string methodName, ByteString args)
public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
public void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress)
public Address GetZeroSmartContractAddress()
public Address GetZeroSmartContractAddress(int chainId)
public Address GetContractAddressByName(Hash hash)
public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
public byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
public byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)

