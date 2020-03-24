<a name='assembly'></a>
# AElf.Sdk.CSharp

## Contents

- [CSharpSmartContractContext](#T-AElf-Sdk-CSharp-CSharpSmartContractContext 'AElf.Sdk.CSharp.CSharpSmartContractContext')
  - [ChainId](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId 'AElf.Sdk.CSharp.CSharpSmartContractContext.ChainId')
  - [StateProvider](#P-AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider 'AElf.Sdk.CSharp.CSharpSmartContractContext.StateProvider')
  - [LogDebug(func)](#M-AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-Func{System-String}- 'AElf.Sdk.CSharp.CSharpSmartContractContext.LogDebug(System.Func{System.String})')
- [SmartContractConstants](#T-AElf-Sdk-CSharp-SmartContractConstants 'AElf.Sdk.CSharp.SmartContractConstants')

<a name='T-AElf-Sdk-CSharp-CSharpSmartContractContext'></a>
## CSharpSmartContractContext `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

represents the transaction execution context in a smart contract. An instance of this class is present in the
base class for smart contracts (Context property). It provides access to properties and methods useful for
implementing the logic in smart contracts.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-ChainId'></a>
### ChainId `property`

##### Summary

The chain id of the chain on which the contract is currently running.

<a name='P-AElf-Sdk-CSharp-CSharpSmartContractContext-StateProvider'></a>
### StateProvider `property`

##### Summary

Provides access to the underlying state provider.

<a name='M-AElf-Sdk-CSharp-CSharpSmartContractContext-LogDebug-System-Func{System-String}-'></a>
### LogDebug(func) `method`

##### Summary

Application logging - when writing a contract it is useful to be able to log some elements in the
applications log file to simplify development. Note that these logs are only visible when the node
executing the transaction is build in debug mode.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| func | [System.Func{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{System.String}') | the logic that will be executed for logging purposes. |

<a name='T-AElf-Sdk-CSharp-SmartContractConstants'></a>
## SmartContractConstants `type`

##### Namespace

AElf.Sdk.CSharp

##### Summary

Static class containing the hashes built from the names of the contracts.
