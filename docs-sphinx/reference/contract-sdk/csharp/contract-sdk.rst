AElf.Sdk.CSharp
===============


Contents
--------

-  :ref:`BoolState <AElf-Sdk-CSharp-State-BoolState>`
-  :ref:`BytesState <AElf-Sdk-CSharp-State-BytesState>`
-  :ref:`CSharpSmartContractContext <AElf-Sdk-CSharp-CSharpSmartContractContext>`

   -  :ref:`ChainId <AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId>`
   -  :ref:`CurrentBlockTime <AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentBlockTime>`
   -  :ref:`CurrentHeight <AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentHeight>`
   -  :ref:`Origin <AElf-Sdk-CSharp-CSharpSmartContractContext-Origin>`
   -  :ref:`PreviousBlockHash <AElf-Sdk-CSharp-CSharpSmartContractContext-PreviousBlockHash>`
   -  :ref:`Self <AElf-Sdk-CSharp-CSharpSmartContractContext-Self>`
   -  :ref:`Sender <AElf-Sdk-CSharp-CSharpSmartContractContext-Sender>`
   -  :ref:`StateProvider <AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider>`
   -  :ref:`TransactionId <AElf-Sdk-CSharp-CSharpSmartContractContext-TransactionId>`
   -  :ref:`Variables <AElf-Sdk-CSharp-CSharpSmartContractContext-Variables>`
   -  :ref:`Transaction <AElf-Sdk-CSharp-CSharpSmartContractContext-Transaction>`
   -  :ref:`Call(fromAddress,toAddress,methodName,args) <AElf-Sdk-CSharp-CSharpSmartContractContext-Call-AElf-Types-Address-AElf-Types-Address-System-String-Google-Protobuf-ByteString>`
   -  :ref:`ConvertHashToInt64(hash,start,end) <AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertHashToInt64-AElf-Types-Hash-System-Int64-System-Int64>`
   -  :ref:`ConvertVirtualAddressToContractAddress(virtualAddress) <AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash>`
   -  :ref:`ConvertVirtualAddressToContractAddress(virtualAddress,contractAddress) <AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash-AElf-Types-Address>`
   -  :ref:`ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress) <AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash>`
   -  :ref:`ConvertVirtualAddressToContractAddressWithContractHashName(virtualAddress,contractAddress) <AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash-AElf-Types-Address>`
   -  :ref:`DeployContract(address,registration,name) <AElf-Sdk-CSharp-CSharpSmartContractContext-DeployContract-AElf-Types-Address-AElf-Types-SmartContractRegistration-AElf-Types-Hash>`
   -  :ref:`FireLogEvent(logEvent) <AElf-Sdk-CSharp-CSharpSmartContractContext-FireLogEvent-AElf-Types-LogEvent>`
   -  :ref:`GenerateId(contractAddress,bytes) <AElf-Sdk-CSharp-CSharpSmartContractContext-GenerateId-AElf-Types-Address-System-Collections-Generic-IEnumerableSystem-Byte>`
   -  :ref:`GetContractAddressByName(hash) <AElf-Sdk-CSharp-CSharpSmartContractContext-GetContractAddressByName-AElf-Types-Hash>`
   -  :ref:`GetPreviousBlockTransactions() <AElf-Sdk-CSharp-CSharpSmartContractContext-GetPreviousBlockTransactions>`
   -  :ref:`GetRandomHash(fromHash) <AElf-Sdk-CSharp-CSharpSmartContractContext-GetRandomHash-AElf-Types-Hash>`
   -  :ref:`GetSystemContractNameToAddressMapping() <AElf-Sdk-CSharp-CSharpSmartContractContext-GetSystemContractNameToAddressMapping>`
   -  :ref:`GetZeroSmartContractAddress() <AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress>`
   -  :ref:`GetZeroSmartContractAddress(chainId) <AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress-System-Int32>`
   -  :ref:`LogDebug(func) <AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-FuncSystem-String>`
   -  :ref:`RecoverPublicKey() <AElf-Sdk-CSharp-CSharpSmartContractContext-RecoverPublicKey>`
   -  :ref:`Transaction() <AElf-Sdk-CSharp-CSharpSmartContractContext-Transaction>`
   -  :ref:`SendInline(toAddress,methodName,args) <AElf-Sdk-CSharp-CSharpSmartContractContext-SendInline-AElf-Types-Address-System-String-Google-Protobuf-ByteString>`
   -  :ref:`SendVirtualInline(fromVirtualAddress,toAddress,methodName,args) <AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInline-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-ByteString>`
   -  :ref:`SendVirtualInlineBySystemContract(fromVirtualAddress,toAddress,methodName,args) <AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInlineBySystemContract-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-ByteString>`
   -  :ref:`UpdateContract(address,registration,name) <AElf-Sdk-CSharp-CSharpSmartContractContext-UpdateContract-AElf-Types-Address-AElf-Types-SmartContractRegistration-AElf-Types-Hash>`
   -  :ref:`ValidateStateSize(obj) <AElf-Sdk-CSharp-CSharpSmartContractContext-ValidateStateSize-System-Object>`
   -  :ref:`VerifySignature(tx) <AElf-Sdk-CSharp-CSharpSmartContractContext-VerifySignature-AElf-Types-Transaction>`

-  :ref:`CSharpSmartContract <AElf-Sdk-CSharp-CSharpSmartContract>`

   -  :ref:`Context <AElf-Sdk-CSharp-CSharpSmartContract-Context>`
   -  :ref:`State <AElf-Sdk-CSharp-CSharpSmartContract-State>`

-  :ref:`ContractState <AElf-Sdk-CSharp-State-ContractState>`
-  :ref:`Int32State <AElf-Sdk-CSharp-State-Int32State>`
-  :ref:`Int64State <AElf-Sdk-CSharp-State-Int64State>`
-  :ref:`MappedState <AElf-Sdk-CSharp-State-MappedState>`
-  :ref:`SingletonState <AElf-Sdk-CSharp-State-SingletonState>`
-  :ref:`SmartContractBridgeContextExtensions <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions>`

   -  :ref:`Call(context,address,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`
   -  :ref:`Call(context,address,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`
   -  :ref:`Call(context,fromAddress,toAddress,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`
   -  :ref:`Call(context,address,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-ByteString>`
   -  :ref:`ConvertToByteString(message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertToByteString-Google-Protobuf-IMessage>`
   -  :ref:`ConvertVirtualAddressToContractAddress(this,virtualAddress) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddress-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash>`
   -  :ref:`ConvertVirtualAddressToContractAddressWithContractHashName(this,virtualAddress) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash>`
   -  :ref:`Fire(context,eventData) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Fire-AElf-Sdk-CSharp-CSharpSmartContractContext>`
   -  :ref:`GenerateId(this,bytes) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-System-Collections-Generic-IEnumerableSystem-Byte>`
   -  :ref:`GenerateId(this,token) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-System-String>`
   -  :ref:`GenerateId(this,token) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash>`
   -  :ref:`GenerateId(this) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext>`
   -  :ref:`GenerateId(this,address,token) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-AElf-Types-Hash>`
   -  :ref:`SendInline(context,toAddress,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`
   -  :ref:`SendInline(context,toAddress,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`
   -  :ref:`SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,message) <AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-IMessage>`

-  :ref:`SmartContractConstants <AElf-Sdk-CSharp-SmartContractConstants>`
-  :ref:`StringState <AElf-Sdk-CSharp-State-StringState>`
-  :ref:`UInt32State <AElf-Sdk-CSharp-State-UInt32State>`
-  :ref:`UInt64State <AElf-Sdk-CSharp-State-UInt64State>`

.. _AElf-Sdk-CSharp-State-BoolState:

BoolState ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around boolean values for use in smart contract state.

.. _AElf-Sdk-CSharp-State-BytesState:

BytesState ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around byte arrays for use in smart contract state.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext:

CSharpSmartContractContext ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp

Summary
'''''''

Represents the transaction execution context in a smart contract. An
instance of this class is present in the base class for smart contracts
(Context property). It provides access to properties and methods useful
for implementing the logic in smart contracts.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId:

ChainId ``property``
>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The chain id of the chain on which the contract is currently running.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentBlockTime:

CurrentBlockTime ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The time included in the current blocks header.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-CurrentHeight:

CurrentHeight ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The height of the block that contains the transaction currently
executing.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Origin:

Origin ``property``
>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The address of the sender (signer) of the transaction being executed.
It’s type is an AElf address. It corresponds to the From field of the
transaction. This value never changes, even for nested inline calls.
This means that when you access this property in your contract, it’s
value will be the entity that created the transaction (user or smart
contract through an inline call).

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-PreviousBlockHash:

PreviousBlockHash ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The hash of the block that precedes the current in the blockchain
structure.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Self:

Self ``property``
>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The address of the contract currently being executed. This changes for
every transaction and inline transaction.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Sender:

Sender ``property``
>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The Sender of the transaction that is executing.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider:

StateProvider ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Provides access to the underlying state provider.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-TransactionId:

TransactionId ``property``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

The ID of the transaction that’s currently executing.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Variables:

Variables ``property``
>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Provides access to variable of the bridge.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Transaction:

Transaction ``property``
>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Including some transaction info.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-Call-AElf-Types-Address-AElf-Types-Address-System-String-Google-Protobuf-ByteString:

Call(fromAddress,toAddress,methodName,args) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Calls a method on another contract.

Returns
'''''''

The result of the call.

Parameters
''''''''''

+--------------+---------------------------+----------------------------------------+
| Name         | Type                      | Description                            |
+==============+===========================+========================================+
| fromAddress  | AElf.Types.Address        | The address to use as sender.          |
+--------------+---------------------------+----------------------------------------+
| toAddress    | AElf.Types.Address        | The address of the contract you’re     |
|              |                           | seeking to interact with.              |
+--------------+---------------------------+----------------------------------------+
| methodName   | System.String             | The name of method you want to call.   |
+--------------+---------------------------+----------------------------------------+
| args         | Google.Protobuf.ByteString| The input arguments for calling that   |
|              |                           | method. This is usually generated from |
|              |                           | the protobuf                           |
+--------------+---------------------------+----------------------------------------+
| definition   |                           |                                        |
| of the input |                           |                                        |
| type         |                           |                                        |
+--------------+---------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ===============================
Name Description
==== ===============================
T    The type of the return message.
==== ===============================

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertHashToInt64-AElf-Types-Hash-System-Int64-System-Int64:

ConvertHashToInt64(hash,start,end) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts the input hash to a 64-bit signed integer.

Returns
'''''''

The 64-bit signed integer.

Parameters
''''''''''

+---------+------------------------------------------------------------------------------------------------------------+-----------------------------------------------------------------------------------------------------------+
| Name    | Type                                                                                                       | Description                                                                                               |
+=========+============================================================================================================+===========================================================================================================+
| hash    | AElf.Types.Hash                                                                                            | The hash.                                                                                                 |
+---------+------------------------------------------------------------------------------------------------------------+-----------------------------------------------------------------------------------------------------------+
| start   | `System.Int64 <http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64>`__   | The inclusive lower bound of the number returned.                                                         |
+---------+------------------------------------------------------------------------------------------------------------+-----------------------------------------------------------------------------------------------------------+
| end     | `System.Int64 <http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64>`__   | The exclusive upper bound of the number returned. endValue must be greater than or equal to startValue.   |
+---------+------------------------------------------------------------------------------------------------------------+-----------------------------------------------------------------------------------------------------------+

Exceptions
''''''''''

+------------------------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------+
| Name                                                                                                                               | Description                                           |
+====================================================================================================================================+=======================================================+
| `System.ArgumentException <http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException>`__   | startValue is less than 0 or greater than endValue.   |
+------------------------------------------------------------------------------------------------------------------------------------+-------------------------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash:

ConvertVirtualAddressToContractAddress(virtualAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address.

Returns
'''''''

The converted address.

Parameters
''''''''''

+----------------+-------------------------+-------------------------+
| Name           | Type                    | Description             |
+================+=========================+=========================+
| virtualAddress | AElf.Types.Hash         | The virtual address     |
|                |                         | that want to convert.   |
+----------------+-------------------------+-------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddress-AElf-Types-Hash-AElf-Types-Address:

ConvertVirtualAddressToContractAddress(virtualAddress,contractAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address with the contract
address.

Returns
'''''''

The converted address.

Parameters
''''''''''

+-----------------+------------------------+------------------------+
| Name            | Type                   | Description            |
+=================+========================+========================+
| virtualAddress  |  AElf.Types.Hash       | The virtual address    |
|                 |                        | that want to convert.  |
+-----------------+------------------------+------------------------+
| contractAddress | AElf.Types.Address     | The contract address.  |
+-----------------+------------------------+------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash:

ConvertVirtualAddressToContractAddressWithContractHashName(
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
virtualAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address with the current
contract hash name.

Returns
'''''''

The converted address.

Parameters
''''''''''

+----------------+-------------------------+-------------------------+
| Name           | Type                    | Description             |
+================+=========================+=========================+
| virtualAddress |  AElf.Types.Hash        | The virtual address     |
|                |                         | that want to convert.   |
+----------------+-------------------------+-------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Types-Hash-AElf-Types-Address:

ConvertVirtualAddressToContractAddressWithContractHashName(
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
virtualAddress,contractAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address with the contract hash
name.

Returns
'''''''

Parameters
''''''''''

+-----------------+------------------------+------------------------+
| Name            | Type                   | Description            |
+=================+========================+========================+
| virtualAddress  | AElf.Types.Hash        | The virtual address    |
|                 |                        | that want to convert.  |
+-----------------+------------------------+------------------------+
| contractAddress | AElf.Types.Address     | The contract address.  |
+-----------------+------------------------+------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-DeployContract-AElf-Types-Address-AElf-Types-SmartContractRegistration-AElf-Types-Hash:

DeployContract(address,registration,name) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Deploy a new smart contract (only the genesis contract can call it).

Parameters
''''''''''

+----------------+--------------------------------------------------------------------------------------+-----------------------------------------------+
| Name           | Type                                                                                 | Description                                   |
+================+======================================================================================+===============================================+
| address        | AElf.Types.Address                                                                   | The address of new smart contract.            |
+----------------+--------------------------------------------------------------------------------------+-----------------------------------------------+
| registration   | AElf.Types.SmartContractRegistration                                                 | The registration of the new smart contract.   |
+----------------+--------------------------------------------------------------------------------------+-----------------------------------------------+
| name           | AElf.Types.Hash                                                                      | The hash value of the smart contract name.    |
+----------------+--------------------------------------------------------------------------------------+-----------------------------------------------+


.. _AElf-Sdk-CSharp-CSharpSmartContractContext-FireLogEvent-AElf-Types-LogEvent:

FireLogEvent(logEvent) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

This method is used to produce logs that can be found in the transaction
result after execution.

Parameters
''''''''''

+----------+------------------------------------+--------------------+
| Name     | Type                               | Description        |
+==========+====================================+====================+
| logEvent | AElf.Types.LogEvent                | The event to fire. |
+----------+------------------------------------+--------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GenerateId-AElf-Types-Address-System-Collections-Generic-IEnumerableSystem-Byte:

GenerateId(contractAddress,bytes) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the contract address and the bytes.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+----------------+-------------------------+----------------------------------------+
| Name           | Type                    | Description                            |
+================+=========================+========================================+
| contractAddress| AElf.Types.Address      | The contract address on which the id   |
|                |                         | generation is based.                   |
+----------------+-------------------------+----------------------------------------+
| bytes          | `System.Collections.    | The bytes on which the id generation   |
|                | Generic.IEnumerable     | is based.                              |
|                | {System.Byte} <http://m |                                        |
|                | sdn.microsoft.com/quer  |                                        |
|                | y/dev14.query?appId=De  |                                        |
|                | v14IDEF1&l=EN-US&k=k:S  |                                        |
|                | ystem.Collections.Gene  |                                        |
|                | ric.IEnumerable>`__     |                                        |
+----------------+-------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetContractAddressByName-AElf-Types-Hash:

GetContractAddressByName(hash) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

It’s sometimes useful to get the address of a system contract. The input
is a hash of the system contracts name. These hashes are easily
accessible through the constants in the SmartContractConstants.cs file
of the C# SDK.

Returns
'''''''

The address of the system contract.

Parameters
''''''''''

==== ======================================== =====================
Name Type                                     Description
==== ======================================== =====================
hash AElf.Types.Hash                          The hash of the name.
==== ======================================== =====================

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetPreviousBlockTransactions:

GetPreviousBlockTransactions() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Returns the transaction included in the previous block (previous to the
one currently executing).

Returns
'''''''

A list of transaction.

Parameters
''''''''''

This method has no parameters.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetRandomHash-AElf-Types-Hash:

GetRandomHash(fromHash) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Gets a random hash based on the input hash.

Returns
'''''''

Random hash.

Parameters
''''''''''

+------------+--------------------------------------------+---------------+
| Name       | Type                                       | Description   |
+============+============================================+===============+
| fromHash   | AElf.Types.Hash                            | Hash.         |
+------------+--------------------------------------------+---------------+


.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetSystemContractNameToAddressMapping:

GetSystemContractNameToAddressMapping() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Get the mapping that associates the system contract addresses and their
name’s hash.

Returns
'''''''

The addresses with their hashes.

Parameters
''''''''''

This method has no parameters.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress:

GetZeroSmartContractAddress() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

This method returns the address of the Genesis contract (smart contract
zero) of the current chain.

Returns
'''''''

The address of the genesis contract.

Parameters
''''''''''

This method has no parameters.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-GetZeroSmartContractAddress-System-Int32:

GetZeroSmartContractAddress(chainId) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

This method returns the address of the Genesis contract (smart contract
zero) of the specified chain.

Returns
'''''''

The address of the genesis contract, for the given chain.

Parameters
''''''''''

+---------+----------------------------------------+-----------------+
| Name    | Type                                   | Description     |
+=========+========================================+=================+
| chainId | `System.Int32 <http://msdn.m           | The chain’s ID. |
|         | icrosoft.com/query/dev14.query?appId=D |                 |
|         | ev14IDEF1&l=EN-US&k=k:System.Int32>`__ |                 |
+---------+----------------------------------------+-----------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-FuncSystem-String:

LogDebug(func) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Application logging - when writing a contract it is useful to be able to
log some elements in the applications log file to simplify development.
Note that these logs are only visible when the node executing the
transaction is build in debug mode.

Parameters
''''''''''

+--------------+-----------------+----------------------------------------+
| Name         | Type            | Description                            |
+==============+=================+========================================+
| func         | `System.Func    | The logic that will be executed for    |
|              | {System.String} | logging purposes.                      |
|              | <https://docs.m |                                        |
|              | icrosoft.com/en |                                        |
|              | -us/dotnet/api/ |                                        |
|              | system.func-1?v |                                        |
|              | iew=netcore     |                                        |
|              | -6.0>`__        |                                        |
+--------------+-----------------+----------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-RecoverPublicKey:

RecoverPublicKey() ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Recovers the public key of the transaction Sender.

Returns
'''''''

A byte array representing the public key.

Parameters
''''''''''

This method has no parameters.

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-SendInline-AElf-Types-Address-System-String-Google-Protobuf-ByteString:

SendInline(toAddress,methodName,args) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends an inline transaction to another contract.

Parameters
''''''''''

+--------------+------------------+----------------------------------------+
| Name         | Type             | Description                            |
+==============+==================+========================================+
| toAddress    |  AElf.Types.     | The address of the contract you’re     |
|              |  Address         | seeking to interact with.              |
+--------------+------------------+----------------------------------------+
| methodName   | `System.String   | The name of method you want to invoke. |
|              | <http://msdn.mi  |                                        |
|              | crosoft.com/que  |                                        |
|              | ry/dev14.query?  |                                        |
|              | appId=Dev14IDEF  |                                        |
|              | 1&l=EN-US&k=k:S  |                                        |
|              | ystem.String>`__ |                                        |
+--------------+------------------+----------------------------------------+
| args         | Google.Protobuf  | The input arguments for calling that   |
|              | .ByteString      | method. This is usually generated from |
|              |                  | the protobuf                           |
+--------------+------------------+----------------------------------------+
| definition   |                  |                                        |
| of the input |                  |                                        |
| type.        |                  |                                        |
+--------------+------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInline-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-ByteString:

SendVirtualInline(fromVirtualAddress,toAddress,methodName,args) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends a virtual inline transaction to another contract.

Parameters
''''''''''

+--------------------+------------------+----------------------------------------+
| Name               | Type             | Description                            |
+====================+==================+========================================+
| fromVirtualAddress | AElf.Types.Hash  | The virtual address to use as sender.  |
+--------------------+------------------+----------------------------------------+
| toAddress          | AElf.Types.      | The address of the contract you’re     |
|                    | Address          | seeking to interact with.              |
+--------------------+------------------+----------------------------------------+
| methodName         | `System.String   | The name of method you want to invoke. |
|                    | <http://msdn.mic |                                        |
|                    | rosoft.com/query |                                        |
|                    | /dev14.query?app |                                        |
|                    | Id=Dev14IDEF1&l= |                                        |
|                    | EN-US&k=k:System |                                        |
|                    | .String>`__      |                                        |
+--------------------+------------------+----------------------------------------+
| args               | Google.Protobuf  | The input arguments for calling that   |
|                    | .ByteString      | method. This is usually generated from |
|                    |                  | the protobuf                           |
+--------------------+------------------+----------------------------------------+
| definition of the  |                  |                                        |
| input type.        |                  |                                        |
+--------------------+------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-SendVirtualInlineBySystemContract-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-ByteString:

SendVirtualInlineBySystemContract(fromVirtualAddress,toAddress,
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
methodName,args)  ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Like SendVirtualInline but the virtual address us a system smart
contract.

Parameters
''''''''''

+--------------------+------------------+----------------------------------------+
| Name               | Type             | Description                            |
+====================+==================+========================================+
| fromVirtualAddress | AElf.Types.Hash  | Sends a virtual inline transaction to  |
|                    |                  | another contract. This method is only  |
|                    |                  | available to system smart contract.    |
+--------------------+------------------+----------------------------------------+
| toAddress          | AElf.Types.      | The address of the contract you’re     |
|                    | Address          | seeking to interact with.              |
+--------------------+------------------+----------------------------------------+
| methodName         | `System.String   | The name of method you want to invoke. |
|                    | <http://msdn.mic |                                        |
|                    | rosoft.com/query |                                        |
|                    | /dev14.query?app |                                        |
|                    | Id=Dev14IDEF1&l= |                                        |
|                    | EN-US&k=k:System |                                        |
|                    | .String>`__      |                                        |
+--------------------+------------------+----------------------------------------+
| args               | Google.Protobuf  | The input arguments for calling that   |
|                    | .ByteString      | method. This is usually generated from |
|                    |                  | the protobuf                           |
+--------------------+------------------+----------------------------------------+
| definition of the  |                  |                                        |
| input type.        |                  |                                        |
+--------------------+------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-UpdateContract-AElf-Types-Address-AElf-Types-SmartContractRegistration-AElf-Types-Hash:

UpdateContract(address,registration,name) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Update a smart contract (only the genesis contract can call it).

Parameters
''''''''''

+----------------+--------------------------------------------------------------------------------------+--------------------------------------------------------+
| Name           | Type                                                                                 | Description                                            |
+================+======================================================================================+========================================================+
| address        | AElf.Types.Address                                                                   | The address of smart contract to update.               |
+----------------+--------------------------------------------------------------------------------------+--------------------------------------------------------+
| registration   | AElf.Types.SmartContractRegistration                                                 | The registration of the smart contract to update.      |
+----------------+--------------------------------------------------------------------------------------+--------------------------------------------------------+
| name           | AElf.Types.Hash <#T-AElf-Types-Hash>                                                 | The hash value of the smart contract name to update.   |
+----------------+--------------------------------------------------------------------------------------+--------------------------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-ValidateStateSize-System-Object:

ValidateStateSize(obj) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Verify that the state size is within the valid value.

Returns
'''''''

The state.

Parameters
''''''''''

+--------+--------------------------------------------------------------------------------------------------------------+---------------+
| Name   | Type                                                                                                         | Description   |
+========+==============================================================================================================+===============+
| obj    | `System.Object <http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object>`__   | The state.    |
+--------+--------------------------------------------------------------------------------------------------------------+---------------+

Exceptions
''''''''''

+--------------------------------------------------------------------------------------------------------------+-------------------------------------+
| Name                                                                                                         | Description                         |
+==============================================================================================================+=====================================+
| AElf.Kernel.SmartContract.StateOverSizeException                                                             | The state size exceeds the limit.   |
+--------------------------------------------------------------------------------------------------------------+-------------------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContractContext-VerifySignature-AElf-Types-Transaction:

VerifySignature(tx) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Returns whether or not the given transaction is well formed and the
signature is correct.

Returns
'''''''

The verification results.

Parameters
''''''''''

+------+------------------------+----------------------------+
| Name | Type                   | Description                |
+======+========================+============================+
| tx   | AElf.Types.Transaction | The transaction to verify. |
+------+------------------------+----------------------------+

.. _AElf-Sdk-CSharp-CSharpSmartContract:

CSharpSmartContract ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp

Summary
'''''''

This class represents a base class for contracts written in the C#
language. The generated code from the protobuf definitions will inherit
from this class.

Generic Types
'''''''''''''

============== ===========
Name           Description
============== ===========
TContractState 
============== ===========

.. _AElf-Sdk-CSharp-CSharpSmartContract-Context:

Context ``property``
>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Represents the transaction execution context in a smart contract. It
provides access inside the contract to properties and methods useful for
implementing the smart contracts action logic.

.. _AElf-Sdk-CSharp-CSharpSmartContract-State:

State ``property``
>>>>>>>>>>>>>>>>>>

Summary
'''''''

Provides access to the State class instance. TContractState is the type
of the state class defined by the contract author.

.. _AElf-Sdk-CSharp-State-ContractState:

ContractState ``type``
>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Base class for the state class in smart contracts.

.. _AElf-Sdk-CSharp-State-Int32State:

Int32State ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around 32-bit integer values for use in smart contract state.

.. _AElf-Sdk-CSharp-State-Int64State:

Int64State ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around 64-bit integer values for use in smart contract state.

.. _AElf-Sdk-CSharp-State-MappedState:

MappedState ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Key-value pair data structure used for representing state in contracts.

Generic Types
'''''''''''''

======= ======================
Name    Description
======= ======================
TKey    The type of the key.
TEntity The type of the value.
======= ======================

.. _AElf-Sdk-CSharp-State-SingletonState:

SingletonState ``type``
>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Represents single values of a given type, for use in smart contract
state.

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions:

SmartContractBridgeContextExtensions ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp

Summary
'''''''

Extension methods that help with the interactions with the smart
contract execution context.

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

Call(context,address,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Calls a method on another contract.

Returns
'''''''

The return value of the call.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | AElf.Kernel.SmartContract.  | The virtual address of the system.     |
|                    | ISmartContractBridgeContext | contract to use as sender.             |
+--------------------+-----------------------------+----------------------------------------+
| address            | AElf.Types.                 | The address of the contract you’re     |
|                    | Address                     | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to call.   |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The input arguments for calling that   |
|                    |                             | method. This is usually generated from |
|                    |                             | the protobuf                           |
+--------------------+-----------------------------+----------------------------------------+
| definition of the  |                             |                                        |
| input type.        |                             |                                        |
+--------------------+-----------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ============================
Name Description
==== ============================
T    The return type of the call.
==== ============================

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

Call(context,address,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Calls a method on another contract.

Returns
'''''''

The result of the call.

Parameters
''''''''''


+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | :ref:`AElf.Sdk.CSharp.CShar\| An instance of                         |
|                    | pSmartContractContext <AElf\| ISmartContractBridgeContext            |
|                    | -Sdk-CSharp-CSharpSmartCont\|                                        |
|                    | ractContext>`               |                                        |
+--------------------+-----------------------------+----------------------------------------+
| address            | AElf.Types.                 | The address of the contract you’re     |
|                    | Address                     | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to call.   |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ===============================
Name Description
==== ===============================
T    The type of the return message.
==== ===============================

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

Call(context,fromAddress,toAddress,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Calls a method on another contract.

Returns
'''''''

The result of the call.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | :ref:`AElf.Sdk.CSharp.CShar\| An instance of                         |
|                    | pSmartContractContext <AElf\| ISmartContractBridgeContext            |
|                    | -Sdk-CSharp-CSharpSmartCont\|                                        |
|                    | ractContext>`               |                                        |
+--------------------+-----------------------------+----------------------------------------+
| fromAddress        | AElf.Types.                 | The address to use as sender.          |
|                    | Address                     |                                        |
+--------------------+-----------------------------+----------------------------------------+
| toAddressvv        | AElf.Types.                 | The address of the contract you’re     |
|                    | Address                     | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to call.   |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ===============================
Name Description
==== ===============================
T    The type of the return message.
==== ===============================

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Call-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-ByteString:

Call(context,address,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Calls a method on another contract.

Returns
'''''''

The result of the call.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | :ref:`AElf.Sdk.CSharp.CShar\| An instance of                         |
|                    | pSmartContractContext <AElf\| ISmartContractBridgeContext            |
|                    | -Sdk-CSharp-CSharpSmartCont\|                                        |
|                    | ractContext>`               |                                        |
+--------------------+-----------------------------+----------------------------------------+
| address            | AElf.Types.                 | The address to use as sender.          |
|                    | Address                     |                                        |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to call.   |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ===============================
Name Description
==== ===============================
T    The type of the return message.
==== ===============================

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertToByteString-Google-Protobuf-IMessage:

ConvertToByteString(message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Serializes a protobuf message to a protobuf ByteString.

Returns
'''''''

ByteString.Empty if the message is null

Parameters
''''''''''

+---------+----------------------------+---------------------------+
| Name    | Type                       | Description               |
+=========+============================+===========================+
| message | Google.Protobuf.IMessage   | The message to serialize. |
+---------+----------------------------+---------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddress-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash:

ConvertVirtualAddressToContractAddress(this,virtualAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address.

Returns
'''''''

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| virtualAddress     | AElf.Types.Hash             | The virtual address that want to       |
|                    | Address                     | convert.                               |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-ConvertVirtualAddressToContractAddressWithContractHashName-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash:

ConvertVirtualAddressToContractAddressWithContractHashName(this,
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
virtualAddress) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Converts a virtual address to a contract address with the currently
running contract address.

Returns
'''''''

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| virtualAddress     | AElf.Types.Hash             | The virtual address that want to       |
|                    | Address                     | convert.                               |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-Fire-AElf-Sdk-CSharp-CSharpSmartContractContext:

Fire(context,eventData) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Logs an event during the execution of a transaction. The event type is
defined in the AElf.CSharp.core project.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | :ref:`AElf.Sdk.CSharp.CShar\| An instance of                         |
|                    | pSmartContractContext <AElf\| ISmartContractBridgeContext            |
|                    | -Sdk-CSharp-CSharpSmartCont\|                                        |
|                    | ractContext>`               |                                        |
+--------------------+-----------------------------+----------------------------------------+
| eventData          |                             | The event to log.                      |
+--------------------+-----------------------------+----------------------------------------+

Generic Types
'''''''''''''

==== ======================
Name Description
==== ======================
T    The type of the event.
==== ======================

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-System-Collections-Generic-IEnumerableSystem-Byte:

GenerateId(this,bytes) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the currently running contract address
and the bytes.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| bytes              | `System.Collections.Generic | The bytes on which the id generation   |
|                    | .IEnumerable{System.Byte}   | is based.                              |
|                    | <http://msdn.microsoft.com/ |                                        |
|                    | query/dev14.query?appId=Dev |                                        |
|                    | 14IDEF1&l=EN-US&k=k:System. |                                        |
|                    | Collections.Generic.IEnumer |                                        |
|                    | able>`__                    |                                        |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-System-String:

GenerateId(this,token) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the currently running contract address
and the token.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| token              | `System.String <http://msdn | The token on which the id generation   |
|                    | .microsoft.com/query/dev14. | is based.                              |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash:

GenerateId(this,token) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the currently running contract address
and the hash type token.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| token              | AElf.Types.Hash             | The hash type token on which the id    |
|                    |                             | generation is based.                   |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext:

GenerateId(this) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the currently running contract address.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-GenerateId-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-AElf-Types-Hash:

GenerateId(this,address,token) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Generate a hash type id based on the address and the bytes.

Returns
'''''''

The generated hash type id.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| this               | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| address            | AElf.Types.Address          | The address on which the id generation |
|                    |                             | is based.                              |
+--------------------+-----------------------------+----------------------------------------+
| token              | AElf.Types.Hash             | The hash type token on which the id    |
|                    |                             | generation is based.                   |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

SendInline(context,toAddress,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends an inline transaction to another contract.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| toAddress          | AElf.Types.Address          | The address of the contract you’re     |
|                    |                             | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to invoke. |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendInline-AElf-Sdk-CSharp-CSharpSmartContractContext-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

SendInline(context,toAddress,methodName,message) ``method``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends a virtual inline transaction to another contract.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| toAddress          | AElf.Types.Address          | The address of the contract you’re     |
|                    |                             | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to invoke. |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractBridgeContextExtensions-SendVirtualInline-AElf-Kernel-SmartContract-ISmartContractBridgeContext-AElf-Types-Hash-AElf-Types-Address-System-String-Google-Protobuf-IMessage:

SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
message) ``method``
>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends a virtual inline transaction to another contract.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| fromVirtualAddress | AElf.Types.Hash             | The virtual address to use as sender.  |
+--------------------+-----------------------------+----------------------------------------+
| toAddress          | AElf.Types.Address          | The address of the contract you’re     |
|                    |                             | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to invoke. |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

.. _BoolState:

SendVirtualInline(context,fromVirtualAddress,toAddress,methodName,
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
message) ``method``
>>>>>>>>>>>>>>>>>>>

Summary
'''''''

Sends a virtual inline transaction to another contract.

Parameters
''''''''''

+--------------------+-----------------------------+----------------------------------------+
| Name               | Type                        | Description                            |
+====================+=============================+========================================+
| context            | AElf.Kernel.SmartContract.  | An instance of                         |
|                    | ISmartContractBridgeContext | ISmartContractBridgeContext            |
+--------------------+-----------------------------+----------------------------------------+
| fromVirtualAddress | AElf.Types.Hash             | The virtual address to use as sender.  |
+--------------------+-----------------------------+----------------------------------------+
| toAddress          | AElf.Types.Address          | The address of the contract you’re     |
|                    |                             | seeking to interact with.              |
+--------------------+-----------------------------+----------------------------------------+
| methodName         | `System.String <http://msdn | The name of method you want to invoke. |
|                    | .microsoft.com/query/dev14. |                                        |
|                    | query?appId=Dev14IDEF1&l=EN |                                        |
|                    | -US&k=k:System.String>`__   |                                        |
+--------------------+-----------------------------+----------------------------------------+
| message            | Google.Protobuf.ByteString  | The protobuf message that will be the  |
|                    |                             | input to the call.                     |
+--------------------+-----------------------------+----------------------------------------+

.. _AElf-Sdk-CSharp-SmartContractConstants:

SmartContractConstants ``type``
>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp

Summary
'''''''

Static class containing the hashes built from the names of the
contracts.

.. _AElf-Sdk-CSharp-State-StringState:

StringState ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around string values for use in smart contract state.

.. _AElf-Sdk-CSharp-State-UInt32State:

UInt32State ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around unsigned 32-bit integer values for use in smart contract
state.

.. _AElf-Sdk-CSharp-State-UInt64State:

UInt64State ``type``
>>>>>>>>>>>>>>>>>>>>

Namespace
'''''''''

AElf.Sdk.CSharp.State

Summary
'''''''

Wrapper around unsigned 64-bit integer values for use in smart contract
state.