# C# contract SDK 

## todo intro

## CSharpSmartContractContext

## Properties

### StateProvider

```csharp
public IStateProvider StateProvider
```

Provides access to the underlying state provider.

### ChainId

```csharp
public int ChainId
```

The chain id of the chain on which the contract is currently running.

### TransactionId

```csharp
public Hash TransactionId
```

The ID of the transaction that's currently executing.

### Sender

```csharp
public Address Sender
```

### Self

```csharp
public Address Self
```

### Origin

```csharp
public Address Origin
```

### CurrentHeight

```csharp
public long CurrentHeight
```

### CurrentBlockTime

```csharp
public Timestamp CurrentBlockTime
```

### PreviousBlockHash

```csharp
public Hash PreviousBlockHash
```

### Variables

```csharp
public ContextVariableDictionary Variables
```

### Methods

#### LogDebug

```csharp
public void LogDebug(Func<string> func)
```

#### FireLogEvent

```csharp
public void FireLogEvent(LogEvent logEvent)
```


#### RecoverPublicKey

```csharp
public byte[] RecoverPublicKey()
```



#### GetPreviousBlockTransactions

```csharp
public List<Transaction> GetPreviousBlockTransactions()
```

#### VerifySignature

```csharp
public bool VerifySignature(Transaction tx)
```

#### DeployContract

```csharp
public void DeployContract(Address address, SmartContractRegistration registration, Hash name)

```
#### UpdateContract

```csharp
public void UpdateContract(Address address, SmartContractRegistration registration, Hash name)
```

#### Call T

```csharp
public T Call<T>(Address address, string methodName, ByteString args) where T : IMessage<T>, new()
```

#### SendInline

```csharp
public void SendInline(Address toAddress, string methodName, ByteString args)
```


#### SendVirtualInline

```csharp
public void SendVirtualInline(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
```

#### SendVirtualInlineBySystemContract

```csharp
public void SendVirtualInlineBySystemContract(Hash fromVirtualAddress, Address toAddress, string methodName, ByteString args)
```

#### ConvertVirtualAddressToContractAddress

```csharp
public Address ConvertVirtualAddressToContractAddress(Hash virtualAddress)
```

#### ConvertVirtualAddressToContractAddressWithContractHashName

```csharp
public Address ConvertVirtualAddressToContractAddressWithContractHashName(Hash virtualAddress)
```

#### GetZeroSmartContractAddress

```csharp
public Address GetZeroSmartContractAddress()
```

#### GetZeroSmartContractAddress

```csharp
public Address GetZeroSmartContractAddress(int chainId)
```

#### GetContractAddressByName

```csharp
public Address GetContractAddressByName(Hash hash)
```

#### GetSystemContractNameToAddressMapping

```csharp
public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
```

#### EncryptMessage

```csharp
public byte[] EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
```

#### DecryptMessage

```csharp
public byte[] DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
```

