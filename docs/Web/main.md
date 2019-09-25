# AELF API 1.0


<a name="overview"></a>
## Overview

### Version information
*Version* : 1.0




<a name="paths"></a>
## Paths

<a name="getblockasync"></a>
### Get information about a given block by block hash. Otionally with the list of its transactions.
```
GET /api/blockChain/block
```


#### Parameters

|Type|Name|Description|Schema|Default|
|---|---|---|---|---|
|**Query**|**blockHash**  <br>*optional*|block hash|string||
|**Query**|**includeTransactions**  <br>*optional*|include transactions or not|boolean|`"false"`|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BlockDto](#blockdto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getblockbyheightasync"></a>
### Get information about a given block by block height. Otionally with the list of its transactions.
```
GET /api/blockChain/blockByHeight
```


#### Parameters

|Type|Name|Description|Schema|Default|
|---|---|---|---|---|
|**Query**|**blockHeight**  <br>*optional*|block height|integer (int64)||
|**Query**|**includeTransactions**  <br>*optional*|include transactions or not|boolean|`"false"`|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BlockDto](#blockdto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getblockheightasync"></a>
### Get the height of the current chain.
```
GET /api/blockChain/blockHeight
```


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|integer (int64)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getblockstateasync"></a>
### Get the current state about a given block
```
GET /api/blockChain/blockState
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**blockHash**  <br>*optional*|block hash|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BlockStateDto](#blockstatedto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getchainstatusasync"></a>
### Get the current status of the block chain.
```
GET /api/blockChain/chainStatus
```


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[ChainStatusDto](#chainstatusdto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getcontractfiledescriptorsetasync"></a>
### Get the protobuf definitions related to a contract
```
GET /api/blockChain/contractFileDescriptorSet
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**address**  <br>*optional*|contract address|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string (byte)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getcurrentroundinformationasync"></a>
### Get AEDPoS latest round information from last block header's consensus extra data of best chain.
```
GET /api/blockChain/currentRoundInformation
```


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[RoundDto](#rounddto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="executerawtransactionasync"></a>
### POST /api/blockChain/executeRawTransaction

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[ExecuteRawTransactionDto](#executerawtransactiondto)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


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


<a name="executetransactionasync"></a>
### Call a read-only method on a contract.
```
POST /api/blockChain/executeTransaction
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[ExecuteTransactionDto](#executetransactiondto)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


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


<a name="getmerklepathbytransactionidasync"></a>
### Get the merkle path of a transaction.
```
GET /api/blockChain/merklePathByTransactionId
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**transactionId**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[MerklePathDto](#merklepathdto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="createrawtransactionasync"></a>
### Creates an unsigned serialized transaction
```
POST /api/blockChain/rawTransaction
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[CreateRawTransactionInput](#createrawtransactioninput)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[CreateRawTransactionOutput](#createrawtransactionoutput)|


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


<a name="getroundfrombase64"></a>
### GET /api/blockChain/roundFromBase64

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**str**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[RoundDto](#rounddto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Deserialization


<a name="sendrawtransactionasync"></a>
### send a transaction
```
POST /api/blockChain/sendRawTransaction
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[SendRawTransactionInput](#sendrawtransactioninput)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[SendRawTransactionOutput](#sendrawtransactionoutput)|


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


<a name="sendtransactionasync"></a>
### Broadcast a transaction
```
POST /api/blockChain/sendTransaction
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[SendTransactionInput](#sendtransactioninput)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[SendTransactionOutput](#sendtransactionoutput)|


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


<a name="sendtransactionsasync"></a>
### Broadcast multiple transactions
```
POST /api/blockChain/sendTransactions
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[SendTransactionsInput](#sendtransactionsinput)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< string > array|


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


<a name="gettaskqueuestatusasync"></a>
### GET /api/blockChain/taskQueueStatus

#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [TaskQueueInfoDto](#taskqueueinfodto) > array|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="gettransactionpoolstatusasync"></a>
### Get the transaction pool status.
```
GET /api/blockChain/transactionPoolStatus
```


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetTransactionPoolStatusOutput](#gettransactionpoolstatusoutput)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="gettransactionresultasync"></a>
### Get the current status of a transaction
```
GET /api/blockChain/transactionResult
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**transactionId**  <br>*optional*|transaction id|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[TransactionResultDto](#transactionresultdto)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="gettransactionresultsasync"></a>
### Get multiple transaction results.
```
GET /api/blockChain/transactionResults
```


#### Parameters

|Type|Name|Description|Schema|Default|
|---|---|---|---|---|
|**Query**|**blockHash**  <br>*optional*|block hash|string||
|**Query**|**limit**  <br>*optional*|limit|integer (int32)|`10`|
|**Query**|**offset**  <br>*optional*|offset|integer (int32)|`0`|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [TransactionResultDto](#transactionresultdto) > array|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* BlockChain


<a name="getnetworkinfoasync"></a>
### Get information about the node’s connection to the network.
```
GET /api/net/networkInfo
```


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetNetworkInfoOutput](#getnetworkinfooutput)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Net


<a name="addpeerasync"></a>
### Attempts to add a node to the connected network nodes
```
POST /api/net/peer
```


#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Body**|**input**  <br>*optional*|[AddPeerInput](#addpeerinput)|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


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


<a name="removepeerasync"></a>
### Attempts to remove a node from the connected network nodes
```
DELETE /api/net/peer
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**address**  <br>*optional*|ip address|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|boolean|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Net


<a name="getpeers"></a>
### Get peer info about the connected network nodes
```
GET /api/net/peers
```


#### Parameters

|Type|Name|Schema|Default|
|---|---|---|---|
|**Query**|**withMetrics**  <br>*optional*|boolean|`"false"`|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< [PeerDto](#peerdto) > array|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Net




<a name="definitions"></a>
## Definitions

<a name="addpeerinput"></a>
### AddPeerInput

|Name|Description|Schema|
|---|---|---|
|**Address**  <br>*optional*|ip address|string|


<a name="blockbodydto"></a>
### BlockBodyDto

|Name|Schema|
|---|---|
|**Transactions**  <br>*optional*|< string > array|
|**TransactionsCount**  <br>*optional*|integer (int32)|


<a name="blockdto"></a>
### BlockDto

|Name|Schema|
|---|---|
|**BlockHash**  <br>*optional*|string|
|**Body**  <br>*optional*|[BlockBodyDto](#blockbodydto)|
|**Header**  <br>*optional*|[BlockHeaderDto](#blockheaderdto)|


<a name="blockheaderdto"></a>
### BlockHeaderDto

|Name|Schema|
|---|---|
|**Bloom**  <br>*optional*|string|
|**ChainId**  <br>*optional*|string|
|**Extra**  <br>*optional*|string|
|**Height**  <br>*optional*|integer (int64)|
|**MerkleTreeRootOfTransactions**  <br>*optional*|string|
|**MerkleTreeRootOfWorldState**  <br>*optional*|string|
|**PreviousBlockHash**  <br>*optional*|string|
|**SignerPubkey**  <br>*optional*|string|
|**Time**  <br>*optional*|string (date-time)|


<a name="blockstatedto"></a>
### BlockStateDto

|Name|Schema|
|---|---|
|**BlockHash**  <br>*optional*|string|
|**BlockHeight**  <br>*optional*|integer (int64)|
|**Changes**  <br>*optional*|< string, string > map|
|**PreviousHash**  <br>*optional*|string|


<a name="chainstatusdto"></a>
### ChainStatusDto

|Name|Schema|
|---|---|
|**BestChainHash**  <br>*optional*|string|
|**BestChainHeight**  <br>*optional*|integer (int64)|
|**Branches**  <br>*optional*|< string, integer (int64) > map|
|**ChainId**  <br>*optional*|string|
|**GenesisBlockHash**  <br>*optional*|string|
|**GenesisContractAddress**  <br>*optional*|string|
|**LastIrreversibleBlockHash**  <br>*optional*|string|
|**LastIrreversibleBlockHeight**  <br>*optional*|integer (int64)|
|**LongestChainHash**  <br>*optional*|string|
|**LongestChainHeight**  <br>*optional*|integer (int64)|
|**NotLinkedBlocks**  <br>*optional*|< string, string > map|


<a name="createrawtransactioninput"></a>
### CreateRawTransactionInput

|Name|Description|Schema|
|---|---|---|
|**From**  <br>*required*|from address|string|
|**MethodName**  <br>*required*|contract method name|string|
|**Params**  <br>*required*|contract method parameters|string|
|**RefBlockHash**  <br>*required*|refer block hash|string|
|**RefBlockNumber**  <br>*required*|refer block height|integer (int64)|
|**To**  <br>*required*|to address|string|


<a name="createrawtransactionoutput"></a>
### CreateRawTransactionOutput

|Name|Schema|
|---|---|
|**RawTransaction**  <br>*optional*|string|


<a name="executerawtransactiondto"></a>
### ExecuteRawTransactionDto

|Name|Description|Schema|
|---|---|---|
|**RawTransaction**  <br>*optional*|raw transaction|string|
|**Signature**  <br>*optional*|signature|string|


<a name="executetransactiondto"></a>
### ExecuteTransactionDto

|Name|Description|Schema|
|---|---|---|
|**RawTransaction**  <br>*optional*|raw transaction|string|


<a name="getnetworkinfooutput"></a>
### GetNetworkInfoOutput

|Name|Description|Schema|
|---|---|---|
|**Connections**  <br>*optional*|total number of open connections between this node and other nodes|integer (int32)|
|**ProtocolVersion**  <br>*optional*|network protocol version|integer (int32)|
|**Version**  <br>*optional*|node version|string|


<a name="gettransactionpoolstatusoutput"></a>
### GetTransactionPoolStatusOutput

|Name|Schema|
|---|---|
|**Queued**  <br>*optional*|integer (int32)|
|**Validated**  <br>*optional*|integer (int32)|


<a name="logeventdto"></a>
### LogEventDto

|Name|Schema|
|---|---|
|**Address**  <br>*optional*|string|
|**Indexed**  <br>*optional*|< string > array|
|**Name**  <br>*optional*|string|
|**NonIndexed**  <br>*optional*|string|


<a name="merklepathdto"></a>
### MerklePathDto

|Name|Schema|
|---|---|
|**MerklePathNodes**  <br>*optional*|< [MerklePathNodeDto](#merklepathnodedto) > array|


<a name="merklepathnodedto"></a>
### MerklePathNodeDto

|Name|Schema|
|---|---|
|**Hash**  <br>*optional*|string|
|**IsLeftChildNode**  <br>*optional*|boolean|


<a name="minerinrounddto"></a>
### MinerInRoundDto

|Name|Schema|
|---|---|
|**ActualMiningTimes**  <br>*optional*|< string (date-time) > array|
|**ExpectedMiningTime**  <br>*optional*|string (date-time)|
|**ImpliedIrreversibleBlockHeight**  <br>*optional*|integer (int64)|
|**InValue**  <br>*optional*|string|
|**MissedBlocks**  <br>*optional*|integer (int64)|
|**Order**  <br>*optional*|integer (int32)|
|**OutValue**  <br>*optional*|string|
|**PreviousInValue**  <br>*optional*|string|
|**ProducedBlocks**  <br>*optional*|integer (int64)|
|**ProducedTinyBlocks**  <br>*optional*|integer (int32)|


<a name="peerdto"></a>
### PeerDto

|Name|Schema|
|---|---|
|**BufferedAnnouncementsCount**  <br>*optional*|integer (int32)|
|**BufferedBlocksCount**  <br>*optional*|integer (int32)|
|**BufferedTransactionsCount**  <br>*optional*|integer (int32)|
|**ConnectionTime**  <br>*optional*|integer (int64)|
|**Inbound**  <br>*optional*|boolean|
|**IpAddress**  <br>*optional*|string|
|**ProtocolVersion**  <br>*optional*|integer (int32)|
|**RequestMetrics**  <br>*optional*|< [RequestMetric](#requestmetric) > array|


<a name="requestmetric"></a>
### RequestMetric

|Name|Schema|
|---|---|
|**Info**  <br>*optional*|string|
|**MethodName**  <br>*optional*|string|
|**RequestTime**  <br>*optional*|[Timestamp](#timestamp)|
|**RoundTripTime**  <br>*optional*|integer (int64)|


<a name="rounddto"></a>
### RoundDto

|Name|Schema|
|---|---|
|**ConfirmedIrreversibleBlockHeight**  <br>*optional*|integer (int64)|
|**ConfirmedIrreversibleBlockRoundNumber**  <br>*optional*|integer (int64)|
|**ExtraBlockProducerOfPreviousRound**  <br>*optional*|string|
|**IsMinerListJustChanged**  <br>*optional*|boolean|
|**RealTimeMinerInformation**  <br>*optional*|< string, [MinerInRoundDto](#minerinrounddto) > map|
|**RoundId**  <br>*optional*|integer (int64)|
|**RoundNumber**  <br>*optional*|integer (int64)|
|**TermNumber**  <br>*optional*|integer (int64)|


<a name="sendrawtransactioninput"></a>
### SendRawTransactionInput

|Name|Description|Schema|
|---|---|---|
|**ReturnTransaction**  <br>*optional*|return transaction detail or not|boolean|
|**Signature**  <br>*optional*|signature|string|
|**Transaction**  <br>*optional*|raw transaction|string|


<a name="sendrawtransactionoutput"></a>
### SendRawTransactionOutput

|Name|Schema|
|---|---|
|**Transaction**  <br>*optional*|[TransactionDto](#transactiondto)|
|**TransactionId**  <br>*optional*|string|


<a name="sendtransactioninput"></a>
### SendTransactionInput

|Name|Description|Schema|
|---|---|---|
|**RawTransaction**  <br>*optional*|raw transaction|string|


<a name="sendtransactionoutput"></a>
### SendTransactionOutput

|Name|Schema|
|---|---|
|**TransactionId**  <br>*optional*|string|


<a name="sendtransactionsinput"></a>
### SendTransactionsInput

|Name|Description|Schema|
|---|---|---|
|**RawTransactions**  <br>*optional*|raw transactions|string|


<a name="taskqueueinfodto"></a>
### TaskQueueInfoDto

|Name|Schema|
|---|---|
|**Name**  <br>*optional*|string|
|**Size**  <br>*optional*|integer (int32)|


<a name="timestamp"></a>
### Timestamp

|Name|Schema|
|---|---|
|**Nanos**  <br>*optional*|integer (int32)|
|**Seconds**  <br>*optional*|integer (int64)|


<a name="transactiondto"></a>
### TransactionDto

|Name|Schema|
|---|---|
|**From**  <br>*optional*|string|
|**MethodName**  <br>*optional*|string|
|**Params**  <br>*optional*|string|
|**RefBlockNumber**  <br>*optional*|integer (int64)|
|**RefBlockPrefix**  <br>*optional*|string|
|**Signature**  <br>*optional*|string|
|**To**  <br>*optional*|string|


<a name="transactionresultdto"></a>
### TransactionResultDto

|Name|Schema|
|---|---|
|**BlockHash**  <br>*optional*|string|
|**BlockNumber**  <br>*optional*|integer (int64)|
|**Bloom**  <br>*optional*|string|
|**Error**  <br>*optional*|string|
|**Logs**  <br>*optional*|< [LogEventDto](#logeventdto) > array|
|**ReadableReturnValue**  <br>*optional*|string|
|**ReturnValue**  <br>*optional*|string|
|**Status**  <br>*optional*|string|
|**Transaction**  <br>*optional*|[TransactionDto](#transactiondto)|
|**TransactionId**  <br>*optional*|string|





