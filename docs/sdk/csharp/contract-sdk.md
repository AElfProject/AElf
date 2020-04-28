<a name='assembly'></a>
# AElf.Sdk.CSharp

## Contents

- [BoolState](#T-AElf-Sdk-CSharp-State-BoolState 'AElf.Sdk.CSharp.State.BoolState')
- [BytesState](#T-AElf-Sdk-CSharp-State-BytesState 'AElf.Sdk.CSharp.State.BytesState')
- [CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext')
  - [ChainId](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId 'AElf.Sdk.CSharp.CSharpSmartContractContext.ChainId')
  - [CurrentBlockTime](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentBlockTime 'AElf.Sdk.CSharp.CSharpSmartContractContext.CurrentBlockTime')
  - [CurrentHeight](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentHeight 'AElf.Sdk.CSharp.CSharpSmartContractContext.CurrentHeight')
  - [Origin](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-Origin 'AElf.Sdk.CSharp.CSharpSmartContractContext.Origin')
  - [PreviousBlockHash](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-PreviousBlockHash 'AElf.Sdk.CSharp.CSharpSmartContractContext.PreviousBlockHash')
  - [Self](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-Self 'AElf.Sdk.CSharp.CSharpSmartContractContext.Self')
  - [Sender](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-Sender 'AElf.Sdk.CSharp.CSharpSmartContractContext.Sender')
  - [StateProvider](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider 'AElf.Sdk.CSharp.CSharpSmartContractContext.StateProvider')
  - [TransactionId](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-TransactionId 'AElf.Sdk.CSharp.CSharpSmartContractContext.TransactionId')
  - [Variables](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-Variables 'AElf.Sdk.CSharp.CSharpSmartContractContext.Variables')
  - [Call\`\`1(fromAddress,toAddress,methodName,args)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-Call``1-AElf-Types-Address,AElf-Types-Address,System-String,Google-Protobuf-ByteString- 'AElf.Sdk.CSharp.CSharpSmartContractContext.Call``1(AElf.Types.Address,AElf.Types.Address,System.String,Google.Protobuf.ByteString)')
  - [ConvertVirtualAddressToContractAddress(virtualAddress)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash- 'AElf.Sdk.CSharp.CSharpSmartContractContext.ConvertVirtualAddressToContractAddress(AElf.Types.Hash)')
  - [ConvertVirtualAddressToContractAddress(virtualAddress,contractAddress)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash,AElf-Types-Address- 'AElf.Sdk.CSharp.CSharpSmartContractContext.ConvertVirtualAddressToContractAddress(AElf.Types.Hash,AElf.Types.Address)')
  - [ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash- 'AElf.Sdk.CSharp.CSharpSmartContractContext.ConvertVirtualAddressToContractAddressWithContractHashName(AElf.Types.Hash)')
  - [ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress,contractAddress)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash,AElf-Types-Address- 'AElf.Sdk.CSharp.CSharpSmartContractContext.ConvertVirtualAddressToContractAddressWithContractHashName(AElf.Types.Hash,AElf.Types.Address)')
  - [DecryptMessage(senderPublicKey,cipherMessage)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-DecryptMessage-System-Byte[],System-Byte[]- 'AElf.Sdk.CSharp.CSharpSmartContractContext.DecryptMessage(System.Byte[],System.Byte[])')
  - [EncryptMessage(receiverPublicKey,plainMessage)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-EncryptMessage-System-Byte[],System-Byte[]- 'AElf.Sdk.CSharp.CSharpSmartContractContext.EncryptMessage(System.Byte[],System.Byte[])')
  - [FireLogEvent(logEvent)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-FireLogEvent-AElf-Types-LogEvent- 'AElf.Sdk.CSharp.CSharpSmartContractContext.FireLogEvent(AElf.Types.LogEvent)')
  - [GenerateId(contractAddress,bytes)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GenerateId-AElf-Types-Address,System-Collections-Generic-IEnumerable{System-Byte}- 'AElf.Sdk.CSharp.CSharpSmartContractContext.GenerateId(AElf.Types.Address,System.Collections.Generic.IEnumerable{System.Byte})')
  - [GetContractAddressByName(hash)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetContractAddressByName-AElf-Types-Hash- 'AElf.Sdk.CSharp.CSharpSmartContractContext.GetContractAddressByName(AElf.Types.Hash)')
  - [GetPreviousBlockTransactions()](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetPreviousBlockTransactions 'AElf.Sdk.CSharp.CSharpSmartContractContext.GetPreviousBlockTransactions')
  - [GetSystemContractNameToAddressMapping()](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetSystemContractNameToAddressMapping 'AElf.Sdk.CSharp.CSharpSmartContractContext.GetSystemContractNameToAddressMapping')
  - [GetZeroSmartContractAddress()](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress 'AElf.Sdk.CSharp.CSharpSmartContractContext.GetZeroSmartContractAddress')
  - [GetZeroSmartContractAddress(chainId)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress-System-Int32- 'AElf.Sdk.CSharp.CSharpSmartContractContext.GetZeroSmartContractAddress(System.Int32)')
  - [LogDebug(func)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-Func{System-String}- 'AElf.Sdk.CSharp.CSharpSmartContractContext.LogDebug(System.Func{System.String})')
  - [RecoverPublicKey()](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-RecoverPublicKey 'AElf.Sdk.CSharp.CSharpSmartContractContext.RecoverPublicKey')
  - [SendInline(toAddress,methodName,args)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendInline-AElf-Types-Address,System-String,Google-Protobuf-ByteString- 'AElf.Sdk.CSharp.CSharpSmartContractContext.SendInline(AElf.Types.Address,System.String,Google.Protobuf.ByteString)')
  - [SendVirtualInline(fromVirtualAddress,toAddress,methodName,args)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInline-AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-ByteString- 'AElf.Sdk.CSharp.CSharpSmartContractContext.SendVirtualInline(AElf.Types.Hash,AElf.Types.Address,System.String,Google.Protobuf.ByteString)')
  - [SendVirtualInlineBySystemContract(fromVirtualAddress,toAddress,methodName,args)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInlineBySystemContract-AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-ByteString- 'AElf.Sdk.CSharp.CSharpSmartContractContext.SendVirtualInlineBySystemContract(AElf.Types.Hash,AElf.Types.Address,System.String,Google.Protobuf.ByteString)')
  - [VerifySignature(tx)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-VerifySignature-AElf-Types-Transaction- 'AElf.Sdk.CSharp.CSharpSmartContractContext.VerifySignature(AElf.Types.Transaction)')
- [CSharpSmartContract\`1](#T-AElf-Sdk-CSharp-CSharpSmartContract`1 'AElf.Sdk.CSharp.CSharpSmartContract`1')
  - [Context](#P-AElf-Sdk-CSharp-CSharpSmartContract`1-Context 'AElf.Sdk.CSharp.CSharpSmartContract`1.Context')
  - [State](#P-AElf-Sdk-CSharp-CSharpSmartContract`1-State 'AElf.Sdk.CSharp.CSharpSmartContract`1.State')
- [ContractState](#T-AElf-Sdk-CSharp-State-ContractState 'AElf.Sdk.CSharp.State.ContractState')
- [Int32State](#T-AElf-Sdk-CSharp-State-Int32State 'AElf.Sdk.CSharp.State.Int32State')
- [Int64State](#T-AElf-Sdk-CSharp-State-Int64State 'AElf.Sdk.CSharp.State.Int64State')
- [MappedState\`2](#T-AElf-Sdk-CSharp-State-MappedState`2 'AElf.Sdk.CSharp.State.MappedState`2')
- [SingletonState\`1](#T-AElf-Sdk-CSharp-State-SingletonState`1 'AElf.Sdk.CSharp.State.SingletonState`1')
- [SmartContractBridgeContextExtensions](#T-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions')
  - [Call\`\`1(context,address,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.Call``1(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [Call\`\`1(context,address,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.Call``1(AElf.Sdk.CSharp.CSharpSmartContractContext,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [Call\`\`1(context,fromAddress,toAddress,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.Call``1(AElf.Sdk.CSharp.CSharpSmartContractContext,AElf.Types.Address,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [Call\`\`1(context,address,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-ByteString- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.Call``1(AElf.Sdk.CSharp.CSharpSmartContractContext,AElf.Types.Address,System.String,Google.Protobuf.ByteString)')
  - [ConvertToByteString(message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertToByteString-Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.ConvertToByteString(Google.Protobuf.IMessage)')
  - [ConvertVirtualAddressToContractAddress(this,virtualAddress)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddress-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.ConvertVirtualAddressToContractAddress(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Hash)')
  - [ConvertVirtualAddressToContractAddressWithContractHashName(this,virtualAddress)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.ConvertVirtualAddressToContractAddressWithContractHashName(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Hash)')
  - [Fire\`\`1(context,eventData)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Fire``1-AElf-Sdk-CSharp-CSharpSmartContractContext,``0- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.Fire``1(AElf.Sdk.CSharp.CSharpSmartContractContext,``0)')
  - [GenerateId(this,bytes)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,System-Collections-Generic-IEnumerable{System-Byte}- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.GenerateId(AElf.Kernel.SmartContract.ISmartContractBridgeContext,System.Collections.Generic.IEnumerable{System.Byte})')
  - [GenerateId(this,token)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,System-String- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.GenerateId(AElf.Kernel.SmartContract.ISmartContractBridgeContext,System.String)')
  - [GenerateId(this,token)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.GenerateId(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Hash)')
  - [GenerateId(this)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.GenerateId(AElf.Kernel.SmartContract.ISmartContractBridgeContext)')
  - [GenerateId(this,address,token)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,AElf-Types-Hash- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.GenerateId(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Address,AElf.Types.Hash)')
  - [SendInline(context,toAddress,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.SendInline(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [SendInline(context,toAddress,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.SendInline(AElf.Sdk.CSharp.CSharpSmartContractContext,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.SendVirtualInline(AElf.Kernel.SmartContract.ISmartContractBridgeContext,AElf.Types.Hash,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
  - [SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,message)](#M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-IMessage- 'AElf.Sdk.CSharp.SmartContractBridgeContextExtensions.SendVirtualInline(AElf.Sdk.CSharp.CSharpSmartContractContext,AElf.Types.Hash,AElf.Types.Address,System.String,Google.Protobuf.IMessage)')
- [SmartContractConstants](#T-AElf-Sdk-CSharp-SmartContractConstants 'AElf.Sdk.CSharp.SmartContractConstants')
- [StringState](#T-AElf-Sdk-CSharp-State-StringState 'AElf.Sdk.CSharp.State.StringState')
- [UInt32State](#T-AElf-Sdk-CSharp-State-UInt32State 'AElf.Sdk.CSharp.State.UInt32State')
- [UInt64State](#T-AElf-Sdk-CSharp-State-UInt64State 'AElf.Sdk.CSharp.State.UInt64State')

<a name='T-AElf-Sdk-CSharp-State-BoolState'></a>
## BoolState `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around boolean values for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-State-BytesState'></a>
## BytesState `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around byte arrays for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-CSharpSmartContractContext'></a>
## CSharpSmartContractContext `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

Represents the transaction execution context in a smart contract. An instance of this class is present in the
base class for smart contracts (Context property). It provides access to properties and methods useful for
implementing the logic in smart contracts.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId'></a>
### ChainId `property`

##### Summary

The chain id of the chain on which the contract is currently running.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentBlockTime'></a>
### CurrentBlockTime `property`

##### Summary

The time included in the current blocks header.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentHeight'></a>
### CurrentHeight `property`

##### Summary

The height of the block that contains the transaction currently executing.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-Origin'></a>
### Origin `property`

##### Summary

The address of the sender (signer) of the transaction being executed. It’s type is an AElf address. It
corresponds to the From field of the transaction. This value never changes, even for nested inline calls.
This means that when you access this property in your contract, it’s value will be the entity that created
the transaction (user or smart contract through an inline call).

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-PreviousBlockHash'></a>
### PreviousBlockHash `property`

##### Summary

The hash of the block that precedes the current in the blockchain structure.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-Self'></a>
### Self `property`

##### Summary

The address of the contract currently being executed. This changes for every transaction and inline transaction.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-Sender'></a>
### Sender `property`

##### Summary

The Sender of the transaction that is executing.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider'></a>
### StateProvider `property`

##### Summary

Provides access to the underlying state provider.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-TransactionId'></a>
### TransactionId `property`

##### Summary

The ID of the transaction that's currently executing.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-Variables'></a>
### Variables `property`

##### Summary

Provides access to variable of the bridge.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-Call``1-AElf-Types-Address,AElf-Types-Address,System-String,Google-Protobuf-ByteString-'></a>
### Call\`\`1(fromAddress,toAddress,methodName,args) `method`

##### Summary

Calls a method on another contract.

##### Returns

The result of the call.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| fromAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to call. |
| args | [Google.Protobuf.ByteString](#T-Google-Protobuf-ByteString 'Google.Protobuf.ByteString') | The input arguments for calling that method. This is usually generated from the protobuf
definition of the input type |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The type of the return message. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash-'></a>
### ConvertVirtualAddressToContractAddress(virtualAddress) `method`

##### Summary

Converts a virtual address to a contract address.

##### Returns

The converted address.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash,AElf-Types-Address-'></a>
### ConvertVirtualAddressToContractAddress(virtualAddress,contractAddress) `method`

##### Summary

Converts a virtual address to a contract address with the contract address.

##### Returns

The converted address.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |
| contractAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The contract address. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash-'></a>
### ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress) `method`

##### Summary

Converts a virtual address to a contract address with the current contract hash name.

##### Returns

The converted address.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash,AElf-Types-Address-'></a>
### ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress,contractAddress) `method`

##### Summary

Converts a virtual address to a contract address with the contract hash name.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |
| contractAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The contract address. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-DecryptMessage-System-Byte[],System-Byte[]-'></a>
### DecryptMessage(senderPublicKey,cipherMessage) `method`

##### Summary

Decrypts a message with the given public key.

##### Returns

The decrypted message.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| senderPublicKey | [System.Byte[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Byte[] 'System.Byte[]') | The public key that encrypted the message. |
| cipherMessage | [System.Byte[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Byte[] 'System.Byte[]') | The encrypted message. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-EncryptMessage-System-Byte[],System-Byte[]-'></a>
### EncryptMessage(receiverPublicKey,plainMessage) `method`

##### Summary

Encrypts a message with the given public key.

##### Returns

The encrypted message.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| receiverPublicKey | [System.Byte[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Byte[] 'System.Byte[]') | The receivers public key. |
| plainMessage | [System.Byte[]](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Byte[] 'System.Byte[]') | The non encrypted message. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-FireLogEvent-AElf-Types-LogEvent-'></a>
### FireLogEvent(logEvent) `method`

##### Summary

This method is used to produce logs that can be found in the transaction result after execution.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| logEvent | [AElf.Types.LogEvent](#T-AElf-Types-LogEvent 'AElf.Types.LogEvent') | The event to fire. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GenerateId-AElf-Types-Address,System-Collections-Generic-IEnumerable{System-Byte}-'></a>
### GenerateId(contractAddress,bytes) `method`

##### Summary

Generate a hash type id based on the contract address and the bytes.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| contractAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The contract address on which the id generation is based. |
| bytes | [System.Collections.Generic.IEnumerable{System.Byte}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Byte}') | The bytes on which the id generation is based. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetContractAddressByName-AElf-Types-Hash-'></a>
### GetContractAddressByName(hash) `method`

##### Summary

It's sometimes useful to get the address of a system contract. The input is a hash of the system contracts
name. These hashes are easily accessible through the constants in the SmartContractConstants.cs file of the
C# SDK.

##### Returns

The address of the system contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| hash | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The hash of the name. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetPreviousBlockTransactions'></a>
### GetPreviousBlockTransactions() `method`

##### Summary

Returns the transaction included in the previous block (previous to the one currently executing).

##### Returns

A list of transaction.

##### Parameters

This method has no parameters.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetSystemContractNameToAddressMapping'></a>
### GetSystemContractNameToAddressMapping() `method`

##### Summary

Get the mapping that associates the system contract addresses and their name's hash.

##### Returns

The addresses with their hashes.

##### Parameters

This method has no parameters.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress'></a>
### GetZeroSmartContractAddress() `method`

##### Summary

This method returns the address of the Genesis contract (smart contract zero) of the current chain.

##### Returns

The address of the genesis contract.

##### Parameters

This method has no parameters.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress-System-Int32-'></a>
### GetZeroSmartContractAddress(chainId) `method`

##### Summary

This method returns the address of the Genesis contract (smart contract zero) of the specified chain.

##### Returns

The address of the genesis contract, for the given chain.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| chainId | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The chain's ID. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-Func{System-String}-'></a>
### LogDebug(func) `method`

##### Summary

Application logging - when writing a contract it is useful to be able to log some elements in the
applications log file to simplify development. Note that these logs are only visible when the node
executing the transaction is build in debug mode.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| func | [System.Func{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.String}') | The logic that will be executed for logging purposes. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-RecoverPublicKey'></a>
### RecoverPublicKey() `method`

##### Summary

Recovers the public key of the transaction Sender.

##### Returns

A byte array representing the public key.

##### Parameters

This method has no parameters.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendInline-AElf-Types-Address,System-String,Google-Protobuf-ByteString-'></a>
### SendInline(toAddress,methodName,args) `method`

##### Summary

Sends an inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| args | [Google.Protobuf.ByteString](#T-Google-Protobuf-ByteString 'Google.Protobuf.ByteString') | The input arguments for calling that method. This is usually generated from the protobuf
definition of the input type. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInline-AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-ByteString-'></a>
### SendVirtualInline(fromVirtualAddress,toAddress,methodName,args) `method`

##### Summary

Sends a virtual inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| fromVirtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| args | [Google.Protobuf.ByteString](#T-Google-Protobuf-ByteString 'Google.Protobuf.ByteString') | The input arguments for calling that method. This is usually generated from the protobuf
definition of the input type. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInlineBySystemContract-AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-ByteString-'></a>
### SendVirtualInlineBySystemContract(fromVirtualAddress,toAddress,methodName,args) `method`

##### Summary

Like SendVirtualInline but the virtual address us a system smart contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| fromVirtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address of the system contract to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| args | [Google.Protobuf.ByteString](#T-Google-Protobuf-ByteString 'Google.Protobuf.ByteString') | The input arguments for calling that method. This is usually generated from the protobuf
definition of the input type. |

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-VerifySignature-AElf-Types-Transaction-'></a>
### VerifySignature(tx) `method`

##### Summary

Returns whether or not the given transaction is well formed and the signature is correct.

##### Returns

The verification results.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tx | [AElf.Types.Transaction](#T-AElf-Types-Transaction 'AElf.Types.Transaction') | The transaction to verify. |

<a name='T-AElf-Sdk-CSharp-CSharpSmartContract`1'></a>
## CSharpSmartContract\`1 `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

This class represents a base class for contracts written in the C# language. The generated code from the
protobuf definitions will inherit from this class.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TContractState |  |

<a name='P-AElf-Sdk-CSharp-CSharpSmartContract`1-Context'></a>
### Context `property`

##### Summary

Represents the transaction execution context in a smart contract. It provides access inside the contract to
properties and methods useful for implementing the smart contracts action logic.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContract`1-State'></a>
### State `property`

##### Summary

Provides access to the State class instance. TContractState is the type of the state class defined by the
contract author.

<a name='T-AElf-Sdk-CSharp-State-ContractState'></a>
## ContractState `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Base class for the state class in smart contracts.

<a name='T-AElf-Sdk-CSharp-State-Int32State'></a>
## Int32State `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around 32-bit integer values for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-State-Int64State'></a>
## Int64State `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around 64-bit integer values for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-State-MappedState`2'></a>
## MappedState\`2 `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Key-value pair data structure used for representing state in contracts.

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TKey | The type of the key. |
| TEntity | The type of the value. |

<a name='T-AElf-Sdk-CSharp-State-SingletonState`1'></a>
## SingletonState\`1 `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Represents single values of a given type, for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions'></a>
## SmartContractBridgeContextExtensions `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

Extension methods that help with the interactions with the smart contract execution context.

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### Call\`\`1(context,address,methodName,message) `method`

##### Summary

Calls a method on another contract.

##### Returns

The return value of the call.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| address | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to call. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The return type of the call. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### Call\`\`1(context,address,methodName,message) `method`

##### Summary

Calls a method on another contract.

##### Returns

The result of the call.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| address | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to call. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The type of the return message. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### Call\`\`1(context,fromAddress,toAddress,methodName,message) `method`

##### Summary

Calls a method on another contract.

##### Returns

The result of the call.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| fromAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to call. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The type of the return message. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call``1-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-ByteString-'></a>
### Call\`\`1(context,address,methodName,message) `method`

##### Summary

Calls a method on another contract.

##### Returns

The result of the call.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| address | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to call. |
| message | [Google.Protobuf.ByteString](#T-Google-Protobuf-ByteString 'Google.Protobuf.ByteString') | The protobuf message that will be the input to the call. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The type of the return message. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertToByteString-Google-Protobuf-IMessage-'></a>
### ConvertToByteString(message) `method`

##### Summary

Serializes a protobuf message to a protobuf ByteString.

##### Returns

ByteString.Empty if the message is null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The message to serialize. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddress-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash-'></a>
### ConvertVirtualAddressToContractAddress(this,virtualAddress) `method`

##### Summary

Converts a virtual address to a contract address.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash-'></a>
### ConvertVirtualAddressToContractAddressWithContractHashName(this,virtualAddress) `method`

##### Summary

Converts a virtual address to a contract address with the currently running contract address.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| virtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address that want to convert. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Fire``1-AElf-Sdk-CSharp-CSharpSmartContractContext,``0-'></a>
### Fire\`\`1(context,eventData) `method`

##### Summary

Logs an event during the execution of a transaction. The event type is defined in the AElf.CSharp.core
project.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| eventData | [\`\`0](#T-``0 '``0') | The event to log. |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | The type of the event. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,System-Collections-Generic-IEnumerable{System-Byte}-'></a>
### GenerateId(this,bytes) `method`

##### Summary

Generate a hash type id based on the currently running contract address and the bytes.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| bytes | [System.Collections.Generic.IEnumerable{System.Byte}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.Byte}') | The bytes on which the id generation is based. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,System-String-'></a>
### GenerateId(this,token) `method`

##### Summary

Generate a hash type id based on the currently running contract address and the token.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The token on which the id generation is based. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash-'></a>
### GenerateId(this,token) `method`

##### Summary

Generate a hash type id based on the currently running contract address and the hash type token.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| token | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The hash type token on which the id generation is based. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-'></a>
### GenerateId(this) `method`

##### Summary

Generate a hash type id based on the currently running contract address.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,AElf-Types-Hash-'></a>
### GenerateId(this,address,token) `method`

##### Summary

Generate a hash type id based on the address and the bytes.

##### Returns

The generated hash type id.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| this | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| address | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address on which the id generation is based. |
| token | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The hash type token on which the id generation is based. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### SendInline(context,toAddress,methodName,message) `method`

##### Summary

Sends an inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### SendInline(context,toAddress,methodName,message) `method`

##### Summary

Sends a virtual inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext,AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,message) `method`

##### Summary

Sends a virtual inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Kernel.SmartContract.ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| fromVirtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

<a name='M-AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Sdk-CSharp-CSharpSmartContractContext,AElf-Types-Hash,AElf-Types-Address,System-String,Google-Protobuf-IMessage-'></a>
### SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,message) `method`

##### Summary

Sends a virtual inline transaction to another contract.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| context | [AElf.Sdk.CSharp.CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext') | An instance of [ISmartContractBridgeContext](#T-AElf-Kernel-SmartContract-ISmartContractBridgeContext 'AElf.Kernel.SmartContract.ISmartContractBridgeContext'). |
| fromVirtualAddress | [AElf.Types.Hash](#T-AElf-Types-Hash 'AElf.Types.Hash') | The virtual address to use as sender. |
| toAddress | [AElf.Types.Address](#T-AElf-Types-Address 'AElf.Types.Address') | The address of the contract you're seeking to interact with. |
| methodName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of method you want to invoke. |
| message | [Google.Protobuf.IMessage](#T-Google-Protobuf-IMessage 'Google.Protobuf.IMessage') | The protobuf message that will be the input to the call. |

<a name='T-AElf-Sdk-CSharp-SmartContractConstants'></a>
## SmartContractConstants `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

Static class containing the hashes built from the names of the contracts.

<a name='T-AElf-Sdk-CSharp-State-StringState'></a>
## StringState `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around string values for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-State-UInt32State'></a>
## UInt32State `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around unsigned 32-bit integer values for use in smart contract state.

<a name='T-AElf-Sdk-CSharp-State-UInt64State'></a>
## UInt64State `type`

##### Namespace

AElf.Sdk.CSharp.State

##### Summary

Wrapper around unsigned 64-bit integer values for use in smart contract state.
