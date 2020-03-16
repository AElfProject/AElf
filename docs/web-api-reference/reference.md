# AELF API 1.0


<a name="overview"></a>
## Overview

### Version information
*Version* : 1.0




<a name="paths"></a>
## Paths

<a name="getblockheight"></a>
### GET /api/chain/blockHeight

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

* Chain


<a name="getblockinfo"></a>
### GET /api/chain/blockInfo

#### Parameters

|Type|Name|Schema|Default|
|---|---|---|---|
|**Query**|**blockHashOrHeight**  <br>*optional*|string||
|**Query**|**includeTransactions**  <br>*optional*|boolean|`"false"`|


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

* Chain


<a name="getblockstate"></a>
### GET /api/chain/blockState

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**blockHash**  <br>*optional*|string|


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

* Chain


<a name="broadcasttransaction"></a>
### POST /api/chain/broadcastTransaction

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**rawTransaction**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[BroadcastTransactionOutput](#broadcasttransactionoutput)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Chain


<a name="broadcasttransactions"></a>
### POST /api/chain/broadcastTransactions

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**rawTransactions**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< string > array|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Chain


<a name="call"></a>
### POST /api/chain/call

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**rawTransaction**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Chain


<a name="getchaininformation"></a>
### GET /api/chain/chainInformation

#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|[GetChainInformationOutput](#getchaininformationoutput)|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Chain


<a name="getchainstatus"></a>
### GET /api/chain/chainStatus

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

* Chain


<a name="getfiledescriptorset"></a>
### GET /api/chain/fileDescriptorSet

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**address**  <br>*optional*|string|


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

* Chain


<a name="gettransactionpoolstatus"></a>
### GET /api/chain/transactionPoolStatus

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

* Chain


<a name="gettransactionresult"></a>
### GET /api/chain/transactionResult

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**transactionId**  <br>*optional*|string|


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

* Chain


<a name="gettransactionsresult"></a>
### GET /api/chain/transactionsResult

#### Parameters

|Type|Name|Schema|Default|
|---|---|---|---|
|**Query**|**blockHash**  <br>*optional*|string||
|**Query**|**limit**  <br>*optional*|integer (int32)|`10`|
|**Query**|**offset**  <br>*optional*|integer (int32)|`0`|


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

* Chain


<a name="addpeer"></a>
### POST /api/net/peer

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**address**  <br>*optional*|string|


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


<a name="removepeer"></a>
### DELETE /api/net/peer

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**address**  <br>*optional*|string|


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
### GET /api/net/peers

#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|< string > array|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* Net


<a name="posttest"></a>
### POST /api/versionTest/test

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**test**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* VersionTest


<a name="gettest"></a>
### GET /api/versionTest/test

#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* VersionTest


<a name="deletetest"></a>
### DELETE /api/versionTest/test

#### Parameters

|Type|Name|Schema|
|---|---|---|
|**Query**|**test**  <br>*optional*|string|


#### Responses

|HTTP Code|Description|Schema|
|---|---|---|
|**200**|Success|string|


#### Produces

* `text/plain; v=1.0`
* `application/json; v=1.0`
* `text/json; v=1.0`
* `application/x-protobuf; v=1.0`


#### Tags

* VersionTest




<a name="definitions"></a>
## Definitions

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
|**Time**  <br>*optional*|string (date-time)|


<a name="blockstatedto"></a>
### BlockStateDto

|Name|Schema|
|---|---|
|**BlockHash**  <br>*optional*|string|
|**BlockHeight**  <br>*optional*|integer (int64)|
|**Changes**  <br>*optional*|< string, string > map|
|**PreviousHash**  <br>*optional*|string|


<a name="broadcasttransactionoutput"></a>
### BroadcastTransactionOutput

|Name|Schema|
|---|---|
|**TransactionId**  <br>*optional*|string|


<a name="chainstatusdto"></a>
### ChainStatusDto

|Name|Schema|
|---|---|
|**BestChainHash**  <br>*optional*|string|
|**BestChainHeight**  <br>*optional*|integer (int64)|
|**Branches**  <br>*optional*|< string, integer (int64) > map|
|**GenesisBlockHash**  <br>*optional*|string|
|**LastIrreversibleBlockHash**  <br>*optional*|string|
|**LastIrreversibleBlockHeight**  <br>*optional*|integer (int64)|
|**LongestChainHash**  <br>*optional*|string|
|**LongestChainHeight**  <br>*optional*|integer (int64)|
|**NotLinkedBlocks**  <br>*optional*|< [NotLinkedBlockDto](#notlinkedblockdto) > array|


<a name="getchaininformationoutput"></a>
### GetChainInformationOutput

|Name|Schema|
|---|---|
|**ChainId**  <br>*optional*|string|
|**GenesisContractAddress**  <br>*optional*|string|


<a name="gettransactionpoolstatusoutput"></a>
### GetTransactionPoolStatusOutput

|Name|Schema|
|---|---|
|**Queued**  <br>*optional*|integer (int32)|


<a name="logeventdto"></a>
### LogEventDto

|Name|Schema|
|---|---|
|**Address**  <br>*optional*|string|
|**Indexed**  <br>*optional*|< string > array|
|**Name**  <br>*optional*|string|
|**NonIndexed**  <br>*optional*|string|


<a name="notlinkedblockdto"></a>
### NotLinkedBlockDto

|Name|Schema|
|---|---|
|**BlockHash**  <br>*optional*|string|
|**Height**  <br>*optional*|integer (int64)|
|**PreviousBlockHash**  <br>*optional*|string|


<a name="transactiondto"></a>
### TransactionDto

|Name|Schema|
|---|---|
|**From**  <br>*optional*|string|
|**MethodName**  <br>*optional*|string|
|**Params**  <br>*optional*|string|
|**RefBlockNumber**  <br>*optional*|integer (int64)|
|**RefBlockPrefix**  <br>*optional*|string|
|**Sigs**  <br>*optional*|< string > array|
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
|**Status**  <br>*optional*|string|
|**Transaction**  <br>*optional*|[TransactionDto](#transactiondto)|
|**TransactionId**  <br>*optional*|string|





