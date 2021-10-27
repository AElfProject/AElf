# aelf-sdk.java - AELF Java API

This Java library helps in the communication with an AElf node. You can find out more [**here**](https://github.com/AElfProject/aelf-sdk.java).

## Introduction

aelf-sdk.java is a collection of libraries which allow you to interact with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and running aelf-sdk.java, as well as providing a API reference documentation with examples.

If you need more information you can check out the repo : [aelf-sdk.java](https://github.com/AElfProject/aelf-sdk.java)

## Adding aelf-sdk.java package

First you need to get elf-sdk.java package into your project: [MvnRepository](https://mvnrepository.com/artifact/io.aelf/aelf-sdk)

Maven: 
```
<!-- https://mvnrepository.com/artifact/io.aelf/aelf-sdk -->
<dependency>
    <groupId>io.aelf</groupId>
    <artifactId>aelf-sdk</artifactId>
    <version>0.X.X</version>
</dependency>
```

## Examples

### Create instance

Create a new instance of AElfClient, and set url of an AElf chain node.

```java
using AElf.Client.Service;

// create a new instance of AElf, change the URL if needed
AElfClient client = new AElfClient("http://127.0.0.1:1235");
```

### Test connection

Check that the AElf chain node is connectable.

```java
boolean isConnected = client.isConnected();
```

### Initiate a transfer transaction

```java
// Get token contract address.
String tokenContractAddress = client.getContractAddressByName(privateKey, Sha256.getBytesSha256("AElf.ContractNames.Token"));

Client.Address.Builder to = Client.Address.newBuilder();
to.setValue(ByteString.copyFrom(Base58.decodeChecked("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz")));
Client.Address toObj = to.build();

TokenContract.TransferInput.Builder paramTransfer = TokenContract.TransferInput.newBuilder();
paramTransfer.setTo(toObj);
paramTransfer.setSymbol("ELF");
paramTransfer.setAmount(1000000000);
paramTransfer.setMemo("transfer in demo");
TokenContract.TransferInput paramTransferObj = paramTransfer.build();

String ownerAddress = client.getAddressFromPrivateKey(privateKey);

Transaction.Builder transactionTransfer = client.generateTransaction(ownerAddress, tokenContractAddress, "Transfer", paramTransferObj.toByteArray());
Transaction transactionTransferObj = transactionTransfer.build();
transactionTransfer.setSignature(ByteString.copyFrom(ByteArrayHelper.hexToByteArray(client.signTransaction(privateKey, transactionTransferObj))));
transactionTransferObj = transactionTransfer.build();

// Send the transfer transaction to AElf chain node.
SendTransactionInput sendTransactionInputObj = new SendTransactionInput();
sendTransactionInputObj.setRawTransaction(Hex.toHexString(transactionTransferObj.toByteArray()));
SendTransactionOutput sendResult = client.sendTransaction(sendTransactionInputObj);

Thread.sleep(4000);
// After the transaction is mined, query the execution results.
TransactionResultDto transactionResult = client.getTransactionResult(sendResult.getTransactionId());
System.out.println(transactionResult.getStatus());

// Query account balance.
Client.Address.Builder owner = Client.Address.newBuilder();
owner.setValue(ByteString.copyFrom(Base58.decodeChecked(ownerAddress)));
Client.Address ownerObj = owner.build();

TokenContract.GetBalanceInput.Builder paramGetBalance = TokenContract.GetBalanceInput.newBuilder();
paramGetBalance.setSymbol("ELF");
paramGetBalance.setOwner(ownerObj);
TokenContract.GetBalanceInput paramGetBalanceObj = paramGetBalance.build();

Transaction.Builder transactionGetBalance = client.generateTransaction(ownerAddress, tokenContractAddress, "GetBalance", paramGetBalanceObj.toByteArray());
Transaction transactionGetBalanceObj = transactionGetBalance.build();
String signature = client.signTransaction(privateKey, transactionGetBalanceObj);
transactionGetBalance.setSignature(ByteString.copyFrom(ByteArrayHelper.hexToByteArray(signature)));
transactionGetBalanceObj = transactionGetBalance.build();

ExecuteTransactionDto executeTransactionDto = new ExecuteTransactionDto();
executeTransactionDto.setRawTransaction(Hex.toHexString(transactionGetBalanceObj.toByteArray()));
String transactionGetBalanceResult = client.executeTransaction(executeTransactionDto);

TokenContract.GetBalanceOutput balance = TokenContract.GetBalanceOutput.getDefaultInstance().parseFrom(ByteArrayHelper.hexToByteArray(transactionGetBalanceResult));
System.out.println(balance.getBalance());
```

## Web API

*You can see how the Web Api of the node works in `{chainAddress}/swagger/index.html`*
_tip: for an example, my local address: 'http://127.0.0.1:1235/swagger/index.html'_

The usage of these methods is based on the AElfClient instance, so if you don't have one please create it:

```java
using AElf.Client.Service;

// create a new instance of AElf, change the URL if needed
AElfClient client = new AElfClient("http://127.0.0.1:1235");
```

### GetChainStatus

Get the current status of the block chain.

_Web API path_

`/api/blockChain/chainStatus`

_Parameters_

Empty

_Returns_

`ChainStatusDto`

- `ChainId - String`
- `Branches - HashMap<String, Long>`
- `NotLinkedBlocks - ashMap<String, String>`
- `LongestChainHeight - long`
- `LongestChainHash - String`
- `GenesisBlockHash - String`
- `GenesisContractAddress - String`
- `LastIrreversibleBlockHash - String`
- `LastIrreversibleBlockHeight - long`
- `BestChainHash - String`
- `BestChainHeight - long`

_Example_

```java
client.getChainStatus();
```

### GetContractFileDescriptorSet

Get the protobuf definitions related to a contract.

_Web API path_

`/api/blockChain/contractFileDescriptorSet`

_Parameters_

1. `contractAddress - String` address of a contract

_Returns_

`byte[]`

_Example_
```java
client.getContractFileDescriptorSet(address);
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
```java
client.getBlockHeight();
```

### GetBlock

Get block information by block hash.

_Web API path_

`/api/blockChain/block`

_Parameters_

1. `blockHash - String`
2. `includeTransactions - boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`BlockDto`
- `BlockHash - String`
- `Header - BlockHeaderDto`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - String`
  - `Height - long`
  - `Time - Date`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - List<String>`

_Example_
```java
client.getBlockByHash(blockHash);
```

### GetBlockByHeight

_Web API path_

`/api/blockChain/blockByHeight`

Get block information by block height.

_Parameters_

1. `blockHeight - long`
2. `includeTransactions - boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`BlockDto`
- `BlockHash - String`
- `Header - BlockHeaderDto`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - String`
  - `Height - long`
  - `Time - Date`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - List<String>`

_Example_
```java
client.getBlockByHeight(height);
```

### GetTransactionResult

Get the result of a transaction.

_Web API path_

`/api/blockChain/transactionResult`

_Parameters_

1. `transactionId - String`

_Returns_

`TransactionResultDto`
- `TransactionId - String`
- `Status - String`
- `Logs - ist<LogEventDto>`
  - `Address - String`
  - `Name - String`
  - `Indexed - List<String>`
  - `NonIndexed - String`
- `Bloom - String`
- `BlockNumber - long`
- `Transaction - TransactionDto`
  - `From - String`
  - `To - String`
  - `RefBlockNumber - long`
  - `RefBlockPrefix - String`
  - `MethodName - String`
  - `Params - String`
  - `Signature - String`
- `Error - String`

_Example_
```java
client.getTransactionResult(transactionId);
```

### GetTransactionResults

Get multiple transaction results in a block.

_Web API path_

`/api/blockChain/transactionResults`

_Parameters_

1. `blockHash - String`
2. `offset - int`
3. `limit - int`

_Returns_
  
  `List<TransactionResultDto>` - The array of transaction result:
  - the transaction result object

_Example_
```java
client.getTransactionResults(blockHash, 0, 10);
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
```java
client.getTransactionPoolStatus();
```

### SendTransaction

Broadcast a transaction.

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`SendTransactionInput` - Serialization of data into protobuf data:
- `RawTransaction - String`

_Returns_

`SendTransactionOutput`
- `TransactionId - String`

_Example_
```java
client.sendTransaction(input);
```

### SendRawTransaction

Broadcast a transaction.

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`SendRawTransactionInput` - Serialization of data into protobuf data:
- `Transaction - String`
- `Signature - String`
- `ReturnTransaction - boolean`

_Returns_

`SendRawTransactionOutput`
- `TransactionId - String`
- `Transaction - TransactionDto`

_Example_
```java
client.sendRawTransaction(input);
```

### SendTransactions

Broadcast multiple transactions.

_Web API path_

`/api/blockChain/sendTransactions`

_POST_

_Parameters_

`SendTransactionsInput` - Serialization of data into protobuf data:
- `RawTransactions - String`

_Returns_

`List<String>`

_Example_
```java
client.sendTransactions(input);
```

### CreateRawTransaction

Creates an unsigned serialized transaction.

_Web API path_

`/api/blockChain/rawTransaction`

_POST_

_Parameters_

`CreateRawTransactionInput`
- `From - String`
- `To - String`
- `RefBlockNumber - long`
- `RefBlockHash - String`
- `MethodName - String`
- `Params - String`

_Returns_

`CreateRawTransactionOutput`- Serialization of data into protobuf data:
- `RawTransaction - String`

_Example_
```java
client.createRawTransaction(input);
```

### ExecuteTransaction

Call a read-only method on a contract.

_Web API path_

`/api/blockChain/executeTransaction`

_POST_

_Parameters_

`ExecuteTransactionDto` - Serialization of data into protobuf data:
- `RawTransaction - String`

_Returns_

`String`

_Example_
```java
client.executeTransaction(input);
```

### ExecuteRawTransaction

Call a read-only method on a contract.

_Web API path_

`/api/blockChain/executeRawTransaction`

_POST_

_Parameters_

`ExecuteRawTransactionDto` - Serialization of data into protobuf data:
- `RawTransaction - String`
- `Signature - String`

_Returns_

`String`

_Example_
```java
client.executeRawTransaction(input);
```

### GetPeers

Get peer info about the connected network nodes.

_Web API path_

`/api/net/peers`

_Parameters_

1. `withMetrics - boolean`

_Returns_

`List<PeerDto>`
- `IpAddress - String`
- `ProtocolVersion - int`
- `ConnectionTime - long`
- `ConnectionStatus - String`
- `Inbound - boolean`
- `BufferedTransactionsCount - int`
- `BufferedBlocksCount - int`
- `BufferedAnnouncementsCount - int`
- `RequestMetrics - List<RequestMetric>`
  - `RoundTripTime - long`
  - `MethodName - String`
  - `Info - String`
  - `RequestTime - String`

_Example_
```java
client.getPeers(false);
```

### AddPeer

Attempts to add a node to the connected network nodes.

_Web API path_

`/api/net/peer`

_POST_

_Parameters_

`AddPeerInput`
- `Address - String`

_Returns_

`boolean`

_Example_
```java
client.addPeer("127.0.0.1:7001");
```

### RemovePeer

Attempts to remove a node from the connected network nodes.

_Web API path_

`/api/net/peer`

_DELETE_

_Parameters_

1. `address - String`

_Returns_

`boolean`

_Example_
```java
client.removePeer("127.0.0.1:7001");
```

### GetNetworkInfo

Get the network information of the node.

_Web API path_

`/api/net/networkInfo`

_Parameters_

Empty

_Returns_

`NetworkInfoOutput`
- `Version - String`
- `ProtocolVersion - int`
- `Connections - int`

_Example_
```java
client.getNetworkInfo();
```

## AElf Client

### IsConnected

Verify whether this sdk successfully connects the chain.

_Parameters_

Empty

_Returns_

`boolean`

_Example_
```java
client.isConnected();
```

### GetGenesisContractAddress

Get the address of genesis contract.

_Parameters_

Empty

_Returns_

`String`

_Example_
```java
client.getGenesisContractAddress();
```

### GetContractAddressByName

Get address of a contract by given contractNameHash.

_Parameters_

1. `privateKey - String`
2. `contractNameHash - byte[]`

_Returns_

`String`

_Example_
```java
client.getContractAddressByName(privateKey, contractNameHash);
```

### GenerateTransaction

Build a transaction from the input parameters.

_Parameters_

1. `from - String`
2. `to - String`
3. `methodName - String`
4. `input - byte[]`

_Returns_

`Transaction`

_Example_
```java
client.generateTransaction(from, to, methodName, input);
```

### GetFormattedAddress

Convert the Address to the displayed string：symbol_base58-string_base58-String-chain-id.

_Parameters_

1. `privateKey - String`
2. `address - String`

_Returns_

`String`

_Example_
```java
client.getFormattedAddress(privateKey, address);
```

### SignTransaction

Sign a transaction using private key.

_Parameters_

1. `privateKeyHex - String`
2. `transaction - Transaction`

_Returns_

`String`

_Example_
```java
client.signTransaction(privateKeyHex, transaction);
```

### GetAddressFromPubKey

Get the account address through the public key.

_Parameters_

1. `pubKey - String`

_Returns_

`String`

_Example_
```java
client.getAddressFromPubKey(pubKey);
```

### GetAddressFromPrivateKey

Get the account address through the private key.

_Parameters_

1. `privateKey - String`

_Returns_

`String`

_Example_
```java
client.getAddressFromPrivateKey(privateKey);
```

### GenerateKeyPairInfo

Generate a new account key pair.

_Parameters_

Empty

_Returns_

`KeyPairInfo`
- `PrivateKey - String`
- `PublicKey - String`
- `Address - String`

_Example_
```java
client.generateKeyPairInfo();
```

## Supports

- JDK1.8+
- Log4j2.6.2



