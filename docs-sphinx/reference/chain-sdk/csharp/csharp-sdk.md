# aelf-sdk.cs - AELF C# API

This C# library helps in the communication with an AElf node. You can find out more [**here**](https://github.com/AElfProject/aelf-sdk.cs).

## Introduction

aelf-sdk.cs is a collection of libraries which allow you to interact with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and running aelf-sdk.cs, as well as providing a API reference documentation with examples.

If you need more information you can check out the repo : [aelf-sdk.cs](https://github.com/AElfProject/aelf-sdk.cs)

## Adding aelf-sdk.cs package

First you need to get AElf.Client package into your project. This can be done using the following methods:

Package Manager: 
```
PM> Install-Package AElf.Client
```

.NET CLI
```
> dotnet add package AElf.Client
```

PackageReference
```
<PackageReference Include="AElf.Client" Version="X.X.X" />
```

## Examples

### Create instance

Create a new instance of AElfClient, and set url of an AElf chain node.

```C#
using AElf.Client.Service;

// create a new instance of AElfClient
AElfClient client = new AElfClient("http://127.0.0.1:1235");
```

### Test connection

Check that the AElf chain node is connectable.

```C#
var isConnected = await client.IsConnectedAsync();
```

### Initiate a transfer transaction

```C#
// Get token contract address.
var tokenContractAddress = await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom("AElf.ContractNames.Token"));

var methodName = "Transfer";
var param = new TransferInput
{
    To = new Address {Value = Address.FromBase58("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz").Value},
    Symbol = "ELF",
    Amount = 1000000000,
    Memo = "transfer in demo"
};
var ownerAddress = client.GetAddressFromPrivateKey(PrivateKey);

// Generate a transfer transaction.
var transaction = await client.GenerateTransaction(ownerAddress, tokenContractAddress.ToBase58(), methodName, param);
var txWithSign = client.SignTransaction(PrivateKey, transaction); 

// Send the transfer transaction to AElf chain node.
var result = await client.SendTransactionAsync(new SendTransactionInput
{
    RawTransaction = txWithSign.ToByteArray().ToHex()
});

await Task.Delay(4000);
// After the transaction is mined, query the execution results.
var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
Console.WriteLine(transactionResult.Status);

// Query account balance.
var paramGetBalance = new GetBalanceInput
{
    Symbol = "ELF",
    Owner = new Address {Value = Address.FromBase58(ownerAddress).Value}
};
var transactionGetBalance =await client.GenerateTransaction(ownerAddress, tokenContractAddress.ToBase58(), "GetBalance", paramGetBalance);
var txWithSignGetBalance = client.SignTransaction(PrivateKey, transactionGetBalance);

var transactionGetBalanceResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
{
    RawTransaction = txWithSignGetBalance.ToByteArray().ToHex()
});

var balance = GetBalanceOutput.Parser.ParseFrom(ByteArrayHelper.HexstringToByteArray(transactionGetBalanceResult));
Console.WriteLine(balance.Balance);
```

## Web API

*You can see how the Web Api of the node works in `{chainAddress}/swagger/index.html`*
_tip: for an example, my local address: 'http://127.0.0.1:1235/swagger/index.html'_

The usage of these methods is based on the AElfClient instance, so if you don't have one please create it:

```C#
using AElf.Client.Service;

// create a new instance of AElf, change the URL if needed
private AElfClient client = new AElfClient("http://127.0.0.1:1235");
```

### GetChainStatus

Get the current status of the block chain.

_Web API path_

`/api/blockChain/chainStatus`

_Parameters_

Empty

_Returns_

`ChainStatusDto`

- `ChainId - string`
- `Branches - Dictionary<string,long>`
- `NotLinkedBlocks - Dictionary<string,string>`
- `LongestChainHeight - long`
- `LongestChainHash - string`
- `GenesisBlockHash - string`
- `GenesisContractAddress - string`
- `LastIrreversibleBlockHash - string`
- `LastIrreversibleBlockHeight - long`
- `BestChainHash - string`
- `BestChainHeight - long`

_Example_

```C#
await client.GetChainStatusAsync();
```

### GetContractFileDescriptorSet

Get the protobuf definitions related to a contract.

_Web API path_

`/api/blockChain/contractFileDescriptorSet`

_Parameters_

1. `contractAddress - string` address of a contract

_Returns_

`byte[]`

_Example_
```C#
await client.GetContractFileDescriptorSetAsync(address);
```

### GetBlockHeight

Get current best height of the chain.

_Web API path_

`/api/blockChain/blockHeight`

_Parameters_

Empty

_Returns_

`long`

_Example_
```C#
await client.GetBlockHeightAsync();
```

### GetBlock

Get block information by block hash.

_Web API path_

`/api/blockChain/block`

_Parameters_

1. `blockHash - string`
2. `includeTransactions - bool` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`BlockDto`
- `BlockHash - string`
- `Header - BlockHeaderDto`
  - `PreviousBlockHash - string`
  - `MerkleTreeRootOfTransactions - string`
  - `MerkleTreeRootOfWorldState - string`
  - `Extra - string`
  - `Height - long`
  - `Time - DateTime`
  - `ChainId - string`
  - `Bloom - string`
  - `SignerPubkey - string`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - List<string>`

_Example_
```C#
await client.GetBlockByHashAsync(blockHash);
```

### GetBlockByHeight

_Web API path_

`/api/blockChain/blockByHeight`

Get block information by block height.

_Parameters_

1. `blockHeight - long`
2. `includeTransactions - bool` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`BlockDto`
- `BlockHash - string`
- `Header - BlockHeaderDto`
  - `PreviousBlockHash - string`
  - `MerkleTreeRootOfTransactions - string`
  - `MerkleTreeRootOfWorldState - string`
  - `Extra - string`
  - `Height - long`
  - `Time - DateTime`
  - `ChainId - string`
  - `Bloom - string`
  - `SignerPubkey - string`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - List<string>`

_Example_
```C#
await client.GetBlockByHeightAsync(height);
```

### GetTransactionResult

Get the result of a transaction

_Web API path_

`/api/blockChain/transactionResult`

_Parameters_

1. `transactionId - string`

_Returns_

`TransactionResultDto`
- `TransactionId - string`
- `Status - string`
- `Logs - LogEventDto[]`
  - `Address - string`
  - `Name - string`
  - `Indexed - string[]`
  - `NonIndexed - string`
- `Bloom - string`
- `BlockNumber - long`
- `Transaction - TransactionDto`
  - `From - string`
  - `To - string`
  - `RefBlockNumber - long`
  - `RefBlockPrefix - string`
  - `MethodName - string`
  - `Params - string`
  - `Signature - string`
- `Error - string`

_Example_
```C#
await client.GetTransactionResultAsync(transactionId);
```

### GetTransactionResults

Get multiple transaction results in a block.

_Web API path_

`/api/blockChain/transactionResults`

_Parameters_

1. `blockHash - string`
2. `offset - int`
3. `limit - int`

_Returns_
  
  `List<TransactionResultDto>` - The array of transaction result:
  - the transaction result object

_Example_
```C#
await client.GetTransactionResultsAsync(blockHash, 0, 10);
```

### GetTransactionPoolStatus

Get the transaction pool status.

_Web API path_

`/api/blockChain/transactionPoolStatus`

_Parameters_

Empty

_Returns_

`TransactionPoolStatusOutput`
- `Queued` - int
- `Validated` - int

_Example_
```C#
await client.GetTransactionPoolStatusAsync();
```

### SendTransaction

Broadcast a transaction.

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`SendTransactionInput` - Serialization of data into protobuf data:
- `RawTransaction - string` :

_Returns_

`SendTransactionOutput`
- `TransactionId` - string

_Example_
```C#
await client.SendTransactionAsync(input);
```

### SendRawTransaction

Broadcast a transaction.

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`SendRawTransactionInput` - Serialization of data into protobuf data:
- `Transaction - string`
- `Signature - string`
- `ReturnTransaction - bool`

_Returns_

`SendRawTransactionOutput`
- `TransactionId - string`
- `Transaction - TransactionDto`

_Example_
```C#
await client.SendRawTransactionAsync(input);
```

### SendTransactions

Broadcast multiple transactions.

_Web API path_

`/api/blockChain/sendTransactions`

_POST_

_Parameters_

`SendTransactionsInput` - Serialization of data into protobuf data:
- `RawTransactions - string`

_Returns_

`string[]`

_Example_
```C#
await client.SendTransactionsAsync(input);
```

### CreateRawTransaction

Creates an unsigned serialized transaction.

_Web API path_

`/api/blockChain/rawTransaction`

_POST_

_Parameters_

`CreateRawTransactionInput`
- `From - string`
- `To - string`
- `RefBlockNumber - long`
- `RefBlockHash - string`
- `MethodName - string`
- `Params - string`

_Returns_

`CreateRawTransactionOutput`- Serialization of data into protobuf data:
- `RawTransactions - string`

_Example_
```C#
await client.CreateRawTransactionAsync(input);
```

### ExecuteTransaction

Call a read-only method on a contract.

_Web API path_

`/api/blockChain/executeTransaction`

_POST_

_Parameters_

`ExecuteTransactionDto` - Serialization of data into protobuf data:
- `RawTransaction - string`

_Returns_

`string`

_Example_
```C#
await client.ExecuteTransactionAsync(input);
```

### ExecuteRawTransaction

Call a read-only method on a contract.

_Web API path_

`/api/blockChain/executeRawTransaction`

_POST_

_Parameters_

`ExecuteRawTransactionDto` - Serialization of data into protobuf data:
- `RawTransaction - string`
- `Signature - string`

_Returns_

`string`

_Example_
```C#
await client.ExecuteRawTransactionAsync(input);
```

### GetPeers

Get peer info about the connected network nodes.

_Web API path_

`/api/net/peers`

_Parameters_

1. `withMetrics - bool`

_Returns_

`List<PeerDto>`
- `IpAddress - string`
- `ProtocolVersion - int`
- `ConnectionTime - long`
- `ConnectionStatus - string`
- `Inbound - bool`
- `BufferedTransactionsCount - int`
- `BufferedBlocksCount - int`
- `BufferedAnnouncementsCount - int`
- `RequestMetrics - List<RequestMetric>`
  - `RoundTripTime - long`
  - `MethodName - string`
  - `Info - string`
  - `RequestTime - string`

_Example_
```C#
await client.GetPeersAsync(false);
```

### AddPeer

Attempts to add a node to the connected network nodes.

_Web API path_

`/api/net/peer`

_POST_

_Parameters_

1. `ipAddress - string`

_Returns_

`bool`

_Example_
```C#
await client.AddPeerAsync("127.0.0.1:7001");
```

### RemovePeer

Attempts to remove a node from the connected network nodes.

_Web API path_

`/api/net/peer`

_DELETE_

_Parameters_

1. `ipAddress - string`

_Returns_

`bool`

_Example_
```C#
await client.RemovePeerAsync("127.0.0.1:7001");
```

### GetNetworkInfo

Get the network information of the node.

_Web API path_

`/api/net/networkInfo`

_Parameters_

Empty

_Returns_

`NetworkInfoOutput`
- `Version - string`
- `ProtocolVersion - int`
- `Connections - int`

_Example_
```C#
await client.GetNetworkInfoAsync();
```

## AElf Client

### IsConnected

Verify whether this sdk successfully connects the chain.

_Parameters_

Empty

_Returns_

`bool`

_Example_
```C#
await client.IsConnectedAsync();
```

### GetGenesisContractAddress

Get the address of genesis contract.

_Parameters_

Empty

_Returns_

`string`

_Example_
```C#
await client.GetGenesisContractAddressAsync();
```

### GetContractAddressByName

Get address of a contract by given contractNameHash.

_Parameters_

1. `contractNameHash - Hash`

_Returns_

`Address`

_Example_
```C#
await client.GetContractAddressByNameAsync(contractNameHash);
```

### GenerateTransaction

Build a transaction from the input parameters.

_Parameters_

1. `from - string`
2. `to - string`
3. `methodName - string`
4. `input - IMessage`

_Returns_

`Transaction`

_Example_
```C#
await client.GenerateTransactionAsync(from, to, methodName, input);
```

### GetFormattedAddress

Convert the Address to the displayed string：symbol_base58-string_base58-string-chain-id.

_Parameters_

1. `address - Address`

_Returns_

`string`

_Example_
```C#
await client.GetFormattedAddressAsync(address);
```

### SignTransaction

Sign a transaction using private key.

_Parameters_

1. `privateKeyHex - string`
2. `transaction - Transaction`

_Returns_

`Transaction`

_Example_
```C#
client.SignTransaction(privateKeyHex, transaction);
```

### GetAddressFromPubKey

Get the account address through the public key.

_Parameters_

1. `pubKey - string`

_Returns_

`string`

_Example_
```C#
client.GetAddressFromPubKey(pubKey);
```

### GetAddressFromPrivateKey

Get the account address through the private key.

_Parameters_

1. `privateKeyHex - string`

_Returns_

`string`

_Example_
```C#
client.GetAddressFromPrivateKey(privateKeyHex);
```

### GenerateKeyPairInfo

Generate a new account key pair.

_Parameters_

Empty

_Returns_

`KeyPairInfo`
- `PrivateKey - string`
- `PublicKey - string`
- `Address - string`

_Example_
```C#
client.GenerateKeyPairInfo();
```

## Supports

.NET Standard 2.0



