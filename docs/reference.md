# Web api reference

## Overview

### Version information

_Version_ : 1.0

## Paths

### Get information about a given block by block hash. Optionally with the list of its transactions.

```text
GET /api/blockChain/block
```

#### Parameters

| Type | Name | Description | Schema | Default |
| :--- | :--- | :--- | :--- | :--- |
| **Query** | **blockHash**   _optional_ | block hash | string |  |
| **Query** | **includeTransactions**   _optional_ | include transactions or not | boolean | `"false"` |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [BlockDto](reference.md#blockdto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get information about a given block by block height. Optionally with the list of its transactions.

```text
GET /api/blockChain/blockByHeight
```

#### Parameters

| Type | Name | Description | Schema | Default |
| :--- | :--- | :--- | :--- | :--- |
| **Query** | **blockHeight**   _optional_ | block height | integer \(int64\) |  |
| **Query** | **includeTransactions**   _optional_ | include transactions or not | boolean | `"false"` |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [BlockDto](reference.md#blockdto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the height of the current chain.

```text
GET /api/blockChain/blockHeight
```

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | integer \(int64\) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the current state about a given block

```text
GET /api/blockChain/blockState
```

#### Parameters

| Type | Name | Description | Schema |
| :--- | :--- | :--- | :--- |
| **Query** | **blockHash**   _optional_ | block hash | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [BlockStateDto](reference.md#blockstatedto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the current status of the block chain.

```text
GET /api/blockChain/chainStatus
```

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [ChainStatusDto](reference.md#chainstatusdto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the protobuf definitions related to a contract

```text
GET /api/blockChain/contractFileDescriptorSet
```

#### Parameters

| Type | Name | Description | Schema |
| :--- | :--- | :--- | :--- |
| **Query** | **address**   _optional_ | contract address | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | string \(byte\) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get AEDPoS latest round information from last block header's consensus extra data of best chain.

```text
GET /api/blockChain/currentRoundInformation
```

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [RoundDto](reference.md#rounddto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### POST /api/blockChain/executeRawTransaction

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [ExecuteRawTransactionDto](reference.md#executerawtransactiondto) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | string |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Call a read-only method on a contract.

```text
POST /api/blockChain/executeTransaction
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [ExecuteTransactionDto](reference.md#executetransactiondto) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | string |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the merkle path of a transaction.

```text
GET /api/blockChain/merklePathByTransactionId
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Query** | **transactionId**   _optional_ | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [MerklePathDto](reference.md#merklepathdto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Creates an unsigned serialized transaction

```text
POST /api/blockChain/rawTransaction
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [CreateRawTransactionInput](reference.md#createrawtransactioninput) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [CreateRawTransactionOutput](reference.md#createrawtransactionoutput) |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### GET /api/blockChain/roundFromBase64

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Query** | **str**   _optional_ | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [RoundDto](reference.md#rounddto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* Deserialization

### send a transaction

```text
POST /api/blockChain/sendRawTransaction
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [SendRawTransactionInput](reference.md#sendrawtransactioninput) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [SendRawTransactionOutput](reference.md#sendrawtransactionoutput) |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Broadcast a transaction

```text
POST /api/blockChain/sendTransaction
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [SendTransactionInput](reference.md#sendtransactioninput) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [SendTransactionOutput](reference.md#sendtransactionoutput) |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Broadcast multiple transactions

```text
POST /api/blockChain/sendTransactions
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [SendTransactionsInput](reference.md#sendtransactionsinput) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | &lt; string &gt; array |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### GET /api/blockChain/taskQueueStatus

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | &lt; [TaskQueueInfoDto](reference.md#taskqueueinfodto) &gt; array |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the transaction pool status.

```text
GET /api/blockChain/transactionPoolStatus
```

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [GetTransactionPoolStatusOutput](reference.md#gettransactionpoolstatusoutput) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get the current status of a transaction

```text
GET /api/blockChain/transactionResult
```

#### Parameters

| Type | Name | Description | Schema |
| :--- | :--- | :--- | :--- |
| **Query** | **transactionId**   _optional_ | transaction id | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [TransactionResultDto](reference.md#transactionresultdto) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get multiple transaction results.

```text
GET /api/blockChain/transactionResults
```

#### Parameters

| Type | Name | Description | Schema | Default |
| :--- | :--- | :--- | :--- | :--- |
| **Query** | **blockHash**   _optional_ | block hash | string |  |
| **Query** | **limit**   _optional_ | limit | integer \(int32\) | `10` |
| **Query** | **offset**   _optional_ | offset | integer \(int32\) | `0` |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | &lt; [TransactionResultDto](reference.md#transactionresultdto) &gt; array |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* BlockChain

### Get information about the node’s connection to the network.

```text
GET /api/net/networkInfo
```

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | [GetNetworkInfoOutput](reference.md#getnetworkinfooutput) |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* Net

### Attempts to add a node to the connected network nodes

```text
POST /api/net/peer
```

#### Parameters

| Type | Name | Schema |
| :--- | :--- | :--- |
| **Body** | **input**   _optional_ | [AddPeerInput](reference.md#addpeerinput) |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | boolean |

#### Consumes

* `application/json-patch+json; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/*+json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* Net

### Attempts to remove a node from the connected network nodes

```text
DELETE /api/net/peer
```

#### Parameters

| Type | Name | Description | Schema |
| :--- | :--- | :--- | :--- |
| **Query** | **address**   _optional_ | ip address | string |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | boolean |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* Net

### Get peer info about the connected network nodes

```text
GET /api/net/peers
```

#### Parameters

| Type | Name | Schema | Default |
| :--- | :--- | :--- | :--- |
| **Query** | **withMetrics**   _optional_ | boolean | `"false"` |

#### Responses

| HTTP Code | Description | Schema |
| :--- | :--- | :--- |
| **200** | Success | &lt; [PeerDto](reference.md#peerdto) &gt; array |

#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`

#### Tags

* Net

## Definitions

### AddPeerInput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **Address**   _optional_ | ip address | string |

### BlockBodyDto

| Name | Schema |
| :--- | :--- |
| **Transactions**   _optional_ | &lt; string &gt; array |
| **TransactionsCount**   _optional_ | integer \(int32\) |

### BlockDto

| Name | Schema |
| :--- | :--- |
| **BlockHash**   _optional_ | string |
| **Body**   _optional_ | [BlockBodyDto](reference.md#blockbodydto) |
| **Header**   _optional_ | [BlockHeaderDto](reference.md#blockheaderdto) |

### BlockHeaderDto

| Name | Schema |
| :--- | :--- |
| **Bloom**   _optional_ | string |
| **ChainId**   _optional_ | string |
| **Extra**   _optional_ | string |
| **Height**   _optional_ | integer \(int64\) |
| **MerkleTreeRootOfTransactions**   _optional_ | string |
| **MerkleTreeRootOfWorldState**   _optional_ | string |
| **PreviousBlockHash**   _optional_ | string |
| **SignerPubkey**   _optional_ | string |
| **Time**   _optional_ | string \(date-time\) |

### BlockStateDto

| Name | Schema |
| :--- | :--- |
| **BlockHash**   _optional_ | string |
| **BlockHeight**   _optional_ | integer \(int64\) |
| **Changes**   _optional_ | &lt; string, string &gt; map |
| **PreviousHash**   _optional_ | string |

### ChainStatusDto

| Name | Schema |
| :--- | :--- |
| **BestChainHash**   _optional_ | string |
| **BestChainHeight**   _optional_ | integer \(int64\) |
| **Branches**   _optional_ | &lt; string, integer \(int64\) &gt; map |
| **ChainId**   _optional_ | string |
| **GenesisBlockHash**   _optional_ | string |
| **GenesisContractAddress**   _optional_ | string |
| **LastIrreversibleBlockHash**   _optional_ | string |
| **LastIrreversibleBlockHeight**   _optional_ | integer \(int64\) |
| **LongestChainHash**   _optional_ | string |
| **LongestChainHeight**   _optional_ | integer \(int64\) |
| **NotLinkedBlocks**   _optional_ | &lt; string, string &gt; map |

### CreateRawTransactionInput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **From**   _required_ | from address | string |
| **MethodName**   _required_ | contract method name | string |
| **Params**   _required_ | contract method parameters | string |
| **RefBlockHash**   _required_ | refer block hash | string |
| **RefBlockNumber**   _required_ | refer block height | integer \(int64\) |
| **To**   _required_ | to address | string |

### CreateRawTransactionOutput

| Name | Schema |
| :--- | :--- |
| **RawTransaction**   _optional_ | string |

### ExecuteRawTransactionDto

| Name | Description | Schema |
| :--- | :--- | :--- |
| **RawTransaction**   _optional_ | raw transaction | string |
| **Signature**   _optional_ | signature | string |

### ExecuteTransactionDto

| Name | Description | Schema |
| :--- | :--- | :--- |
| **RawTransaction**   _optional_ | raw transaction | string |

### GetNetworkInfoOutput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **Connections**   _optional_ | total number of open connections between this node and other nodes | integer \(int32\) |
| **ProtocolVersion**   _optional_ | network protocol version | integer \(int32\) |
| **Version**   _optional_ | node version | string |

### GetTransactionPoolStatusOutput

| Name | Schema |
| :--- | :--- |
| **Queued**   _optional_ | integer \(int32\) |
| **Validated**   _optional_ | integer \(int32\) |

### LogEventDto

| Name | Schema |
| :--- | :--- |
| **Address**   _optional_ | string |
| **Indexed**   _optional_ | &lt; string &gt; array |
| **Name**   _optional_ | string |
| **NonIndexed**   _optional_ | string |

### MerklePathDto

| Name | Schema |
| :--- | :--- |
| **MerklePathNodes**   _optional_ | &lt; [MerklePathNodeDto](reference.md#merklepathnodedto) &gt; array |

### MerklePathNodeDto

| Name | Schema |
| :--- | :--- |
| **Hash**   _optional_ | string |
| **IsLeftChildNode**   _optional_ | boolean |

### MinerInRoundDto

| Name | Schema |
| :--- | :--- |
| **ActualMiningTimes**   _optional_ | &lt; string \(date-time\) &gt; array |
| **ExpectedMiningTime**   _optional_ | string \(date-time\) |
| **ImpliedIrreversibleBlockHeight**   _optional_ | integer \(int64\) |
| **InValue**   _optional_ | string |
| **MissedBlocks**   _optional_ | integer \(int64\) |
| **Order**   _optional_ | integer \(int32\) |
| **OutValue**   _optional_ | string |
| **PreviousInValue**   _optional_ | string |
| **ProducedBlocks**   _optional_ | integer \(int64\) |
| **ProducedTinyBlocks**   _optional_ | integer \(int32\) |

### PeerDto

| Name | Schema |
| :--- | :--- |
| **BufferedAnnouncementsCount**   _optional_ | integer \(int32\) |
| **BufferedBlocksCount**   _optional_ | integer \(int32\) |
| **BufferedTransactionsCount**   _optional_ | integer \(int32\) |
| **ConnectionTime**   _optional_ | integer \(int64\) |
| **Inbound**   _optional_ | boolean |
| **IpAddress**   _optional_ | string |
| **ProtocolVersion**   _optional_ | integer \(int32\) |
| **RequestMetrics**   _optional_ | &lt; [RequestMetric](reference.md#requestmetric) &gt; array |

### RequestMetric

| Name | Schema |
| :--- | :--- |
| **Info**   _optional_ | string |
| **MethodName**   _optional_ | string |
| **RequestTime**   _optional_ | [Timestamp](reference.md#timestamp) |
| **RoundTripTime**   _optional_ | integer \(int64\) |

### RoundDto

| Name | Schema |
| :--- | :--- |
| **ConfirmedIrreversibleBlockHeight**   _optional_ | integer \(int64\) |
| **ConfirmedIrreversibleBlockRoundNumber**   _optional_ | integer \(int64\) |
| **ExtraBlockProducerOfPreviousRound**   _optional_ | string |
| **IsMinerListJustChanged**   _optional_ | boolean |
| **RealTimeMinerInformation**   _optional_ | &lt; string, [MinerInRoundDto](reference.md#minerinrounddto) &gt; map |
| **RoundId**   _optional_ | integer \(int64\) |
| **RoundNumber**   _optional_ | integer \(int64\) |
| **TermNumber**   _optional_ | integer \(int64\) |

### SendRawTransactionInput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **ReturnTransaction**   _optional_ | return transaction detail or not | boolean |
| **Signature**   _optional_ | signature | string |
| **Transaction**   _optional_ | raw transaction | string |

### SendRawTransactionOutput

| Name | Schema |
| :--- | :--- |
| **Transaction**   _optional_ | [TransactionDto](reference.md#transactiondto) |
| **TransactionId**   _optional_ | string |

### SendTransactionInput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **RawTransaction**   _optional_ | raw transaction | string |

### SendTransactionOutput

| Name | Schema |
| :--- | :--- |
| **TransactionId**   _optional_ | string |

### SendTransactionsInput

| Name | Description | Schema |
| :--- | :--- | :--- |
| **RawTransactions**   _optional_ | raw transactions | string |

### TaskQueueInfoDto

| Name | Schema |
| :--- | :--- |
| **Name**   _optional_ | string |
| **Size**   _optional_ | integer \(int32\) |

### Timestamp

| Name | Schema |
| :--- | :--- |
| **Nanos**   _optional_ | integer \(int32\) |
| **Seconds**   _optional_ | integer \(int64\) |

### TransactionDto

| Name | Schema |
| :--- | :--- |
| **From**   _optional_ | string |
| **MethodName**   _optional_ | string |
| **Params**   _optional_ | string |
| **RefBlockNumber**   _optional_ | integer \(int64\) |
| **RefBlockPrefix**   _optional_ | string |
| **Signature**   _optional_ | string |
| **To**   _optional_ | string |

### TransactionResultDto

| Name | Schema |
| :--- | :--- |
| **BlockHash**   _optional_ | string |
| **BlockNumber**   _optional_ | integer \(int64\) |
| **Bloom**   _optional_ | string |
| **Error**   _optional_ | string |
| **Logs**   _optional_ | &lt; [LogEventDto](reference.md#logeventdto) &gt; array |
| **ReadableReturnValue**   _optional_ | string |
| **ReturnValue**   _optional_ | string |
| **Status**   _optional_ | string |
| **Transaction**   _optional_ | [TransactionDto](reference.md#transactiondto) |
| **TransactionId**   _optional_ | string |

