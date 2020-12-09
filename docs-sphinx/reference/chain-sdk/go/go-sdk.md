# aelf-sdk.go - AELF Go API

This Go library helps in the communication with an AElf node. You can find out more [**here**](https://github.com/AElfProject/aelf-sdk.go).

## Introduction

aelf-sdk.go is a collection of libraries which allow you to interact with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and running aelf-sdk.go, as well as providing a API reference documentation with examples.

If you need more information you can check out the repo : [aelf-sdk.go](https://github.com/AElfProject/aelf-sdk.go)

## Adding aelf-sdk.go package

First you need to get aelf-sdk.go:

```
> go get -u github.com/AElfProject/aelf-sdk.go
```

## Examples

### Create instance

Create a new instance of AElfClient, and set url of an AElf chain node.

```go
import ("github.com/AElfProject/aelf-sdk.go/client")

var aelf = client.AElfClient{
	Host:       "http://127.0.0.1:8000",
	Version:    "1.0",
	PrivateKey: "cd86ab6347d8e52bbbe8532141fc59ce596268143a308d1d40fedf385528b458",
}
```

### Initiate a transfer transaction

```go
// Get token contract address.
tokenContractAddress, _ := aelf.GetContractAddressByName("AElf.ContractNames.Token")
fromAddress := aelf.GetAddressFromPrivateKey(aelf.PrivateKey)
methodName := "Transfer"
toAddress, _ := util.Base58StringToAddress("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz")

params := &pb.TransferInput{
	To:     toAddress,
	Symbol: "ELF",
	Amount: 1000000000,
	Memo:   "transfer in demo",
}
paramsByte, _ := proto.Marshal(params)

// Generate a transfer transaction.
transaction, _ := aelf.CreateTransaction(fromAddress, tokenContractAddress, methodName, paramsByte)
signature, _ := aelf.SignTransaction(aelf.PrivateKey, transaction)
transaction.Signature = signature

// Send the transfer transaction to AElf chain node.
transactionByets, _ := proto.Marshal(transaction)
sendResult, _ := aelf.SendTransaction(hex.EncodeToString(transactionByets))

time.Sleep(time.Duration(4) * time.Second)
transactionResult, _ := aelf.GetTransactionResult(sendResult.TransactionID)
fmt.Println(transactionResult)

// Query account balance.
ownerAddress, _ := util.Base58StringToAddress(fromAddress)
getBalanceInput := &pb.GetBalanceInput{
	Symbol: "ELF",
	Owner:  ownerAddress,
}
getBalanceInputByte, _ := proto.Marshal(getBalanceInput)

getBalanceTransaction, _ := aelf.CreateTransaction(fromAddress, tokenContractAddress, "GetBalance", getBalanceInputByte)
getBalanceTransaction.Params = getBalanceInputByte
getBalanceSignature, _ := aelf.SignTransaction(aelf.PrivateKey, getBalanceTransaction)
getBalanceTransaction.Signature = getBalanceSignature

getBalanceTransactionByets, _ := proto.Marshal(getBalanceTransaction)
getBalanceResult, _ := aelf.ExecuteTransaction(hex.EncodeToString(getBalanceTransactionByets))
balance := &pb.GetBalanceOutput{}
getBalanceResultBytes, _ := hex.DecodeString(getBalanceResult)
proto.Unmarshal(getBalanceResultBytes, balance)
fmt.Println(balance)
```

## Web API

*You can see how the Web Api of the node works in `{chainAddress}/swagger/index.html`*
_tip: for an example, my local address: 'http://127.0.0.1:1235/swagger/index.html'_

The usage of these methods is based on the AElfClient instance, so if you don't have one please create it:

```go
import ("github.com/AElfProject/aelf-sdk.go/client")

var aelf = client.AElfClient{
	Host:       "http://127.0.0.1:8000",
	Version:    "1.0",
	PrivateKey: "680afd630d82ae5c97942c4141d60b8a9fedfa5b2864fca84072c17ee1f72d9d",
}
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
- `Branches - map[string]interface{}`
- `NotLinkedBlocks - map[string]interface{}`
- `LongestChainHeight - int64`
- `LongestChainHash - string`
- `GenesisBlockHash - string`
- `GenesisContractAddress - string`
- `LastIrreversibleBlockHash - string`
- `LastIrreversibleBlockHeight - int64`
- `BestChainHash - string`
- `BestChainHeight - int64`

_Example_

```go
chainStatus, err := aelf.GetChainStatus()
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
```go
contractFile, err := aelf.GetContractFileDescriptorSet("pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i")
```

### GetBlockHeight

Get current best height of the chain.

_Web API path_

`/api/blockChain/blockHeight`

_Parameters_

Empty

_Returns_

`float64`

_Example_
```go
height, err := aelf.GetBlockHeight()
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
  - `Height - int64`
  - `Time - string`
  - `ChainId - string`
  - `Bloom - string`
  - `SignerPubkey - string`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - []string`

_Example_
```go
block, err := aelf.GetBlockByHash(blockHash, true)
```

### GetBlockByHeight

_Web API path_

`/api/blockChain/blockByHeight`

Get block information by block height.

_Parameters_

1. `blockHeight - int64`
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
  - `Height - int64`
  - `Time - string`
  - `ChainId - string`
  - `Bloom - string`
  - `SignerPubkey - string`
- `Body - BlockBodyDto`
  - `TransactionsCount - int`
  - `Transactions - []string`

_Example_
```go
block, err := aelf.GetBlockByHeight(100, true)
```

### GetTransactionResult

Get the result of a transaction.

_Web API path_

`/api/blockChain/transactionResult`

_Parameters_

1. `transactionId - string`

_Returns_

`TransactionResultDto`
- `TransactionId - string`
- `Status - string`
- `Logs - []LogEventDto`
  - `Address - string`
  - `Name - string`
  - `Indexed - []string`
  - `NonIndexed - string`
- `Bloom - string`
- `BlockNumber - int64`
- `BlockHash - string`
- `Transaction - TransactionDto`
  - `From - string`
  - `To - string`
  - `RefBlockNumber - int64`
  - `RefBlockPrefix - string`
  - `MethodName - string`
  - `Params - string`
  - `Signature - string`
- `ReturnValue - string`
- `Error - string`

_Example_
```go
transactionResult, err := aelf.GetTransactionResult(transactionID)
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
  
  `[]TransactionResultDto` - The array of transaction result:
  - the transaction result object

_Example_
```go
transactionResults, err := aelf.GetTransactionResults(blockHash, 0, 10)
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
```go
poolStatus, err := aelf.GetTransactionPoolStatus()
```

### SendTransaction

Broadcast a transaction.

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`SendTransactionInput` - Serialization of data into protobuf data:
- `RawTransaction - string`

_Returns_

`SendTransactionOutput`
- `TransactionId - string`

_Example_
```go
sendResult, err := aelf.SendTransaction(input)
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
```go
sendRawResult, err := aelf.SendRawTransaction(input)
```

### SendTransactions

Broadcast multiple transactions.

_Web API path_

`/api/blockChain/sendTransactions`

_POST_

_Parameters_

`rawTransactions - string` - Serialization of data into protobuf data:

_Returns_

`[]interface{}`

_Example_
```go
results, err := aelf.SendTransactions(transactions)
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
- `RefBlockNumber - int64`
- `RefBlockHash - string`
- `MethodName - string`
- `Params - string`

_Returns_

`CreateRawTransactionOutput`- Serialization of data into protobuf data:
- `RawTransactions - string`

_Example_
```go
result, err := aelf.CreateRawTransaction(input)
```

### ExecuteTransaction

Call a read-only method on a contract.

_Web API path_

`/api/blockChain/executeTransaction`

_POST_

_Parameters_

`rawTransaction - string`

_Returns_

`string`

_Example_
```go
executeresult, err := aelf.ExecuteTransaction(rawTransaction)
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
```go
executeRawresult, err := aelf.ExecuteRawTransaction(executeRawinput)
```

### GetPeers

Get peer info about the connected network nodes.

_Web API path_

`/api/net/peers`

_Parameters_

1. `withMetrics - bool`

_Returns_

`[]PeerDto`
- `IpAddress - string`
- `ProtocolVersion - int`
- `ConnectionTime - int64`
- `ConnectionStatus - string`
- `Inbound - bool`
- `BufferedTransactionsCount - int`
- `BufferedBlocksCount - int`
- `BufferedAnnouncementsCount - int`
- `RequestMetrics - []RequestMetric`
  - `RoundTripTime - int64`
  - `MethodName - string`
  - `Info - string`
  - `RequestTime - string`

_Example_
```go
peers, err := aelf.GetPeers(false);
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
```go
addResult, err := aelf.AddPeer("127.0.0.1:7001");
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
```go
removeResult, err := aelf.RemovePeer("127.0.0.1:7001");
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
```go
networkInfo, err := aelf.GetNetworkInfo()
```

## AElf Client

### IsConnected

Verify whether this sdk successfully connects the chain.

_Parameters_

Empty

_Returns_

`bool`

_Example_
```go
isConnected := aelf.IsConnected()
```

### GetGenesisContractAddress

Get the address of genesis contract.

_Parameters_

Empty

_Returns_

`string`

_Example_
```go
contractAddress, err := aelf.GetGenesisContractAddress()
```

### GetContractAddressByName

Get address of a contract by given contractNameHash.

_Parameters_

1. `contractNameHash - string`

_Returns_

`Address`

_Example_
```go
contractAddress, err := aelf.GetContractAddressByName("AElf.ContractNames.Token")
```

### CreateTransaction

Build a transaction from the input parameters.

_Parameters_

1. `from - string`
2. `to - string`
3. `methodName - string`
4. `params - []byte`

_Returns_

`Transaction`

_Example_
```go
transaction, err := aelf.CreateTransaction(fromAddress, toAddress, methodName, param)
```

### GetFormattedAddress

Convert the Address to the displayed string：symbol_base58-string_base58-string-chain-id.

_Parameters_

1. `address - string`

_Returns_

`string`

_Example_
```go
formattedAddress, err := aelf.GetFormattedAddress(address);
```

### SignTransaction

Sign a transaction using private key.

_Parameters_

1. `privateKey - string`
2. `transaction - Transaction`

_Returns_

`[]byte`

_Example_
```go
signature, err := aelf.SignTransaction(privateKey, transaction)
```

### GetAddressFromPubKey

Get the account address through the public key.

_Parameters_

1. `pubKey - string`

_Returns_

`string`

_Example_
```go
address := aelf.GetAddressFromPubKey(pubKey);
```

### GetAddressFromPrivateKey

Get the account address through the private key.

_Parameters_

1. `privateKey - string`

_Returns_

`string`

_Example_
```go
address := aelf.GetAddressFromPrivateKey(privateKey)
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
```go
keyPair := aelf.GenerateKeyPairInfo()
```

## Supports

Go 1.13