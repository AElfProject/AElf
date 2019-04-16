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


<a name="getblockinfobyhash"></a>
### GET /api/chain/blockInfoByHash

#### Parameters

|Type|Name|Schema|Default|
|---|---|---|---|
|**Query**|**blockHash**  <br>*optional*|string||
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


<a name="getblockinfobyheight"></a>
### GET /api/chain/blockInfoByHeight

#### Parameters

|Type|Name|Schema|Default|
|---|---|---|---|
|**Query**|**blockHeight**  <br>*optional*|integer (int64)||
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


## Swagger.json

Online Swagger: http://editor.swagger.io/

```json
{"swagger":"2.0","info":{"version":"1.0","title":"AELF API 1.0"},"paths":{"/api/chain/chainInformation":{"get":{"tags":["Chain"],"operationId":"GetChainInformation","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/GetChainInformationOutput"}}}}},"/api/chain/call":{"post":{"tags":["Chain"],"operationId":"Call","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"rawTransaction","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"type":"string"}}}}},"/api/chain/fileDescriptorSet":{"get":{"tags":["Chain"],"operationId":"GetFileDescriptorSet","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"address","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"format":"byte","type":"string"}}}}},"/api/chain/broadcastTransaction":{"post":{"tags":["Chain"],"operationId":"BroadcastTransaction","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"rawTransaction","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/BroadcastTransactionOutput"}}}}},"/api/chain/broadcastTransactions":{"post":{"tags":["Chain"],"operationId":"BroadcastTransactions","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"rawTransactions","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"uniqueItems":false,"type":"array","items":{"type":"string"}}}}}},"/api/chain/transactionResult":{"get":{"tags":["Chain"],"operationId":"GetTransactionResult","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"transactionId","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/TransactionResultDto"}}}}},"/api/chain/transactionsResult":{"get":{"tags":["Chain"],"operationId":"GetTransactionsResult","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"blockHash","in":"query","required":false,"type":"string"},{"name":"offset","in":"query","required":false,"type":"integer","format":"int32","default":0},{"name":"limit","in":"query","required":false,"type":"integer","format":"int32","default":10}],"responses":{"200":{"description":"Success","schema":{"uniqueItems":false,"type":"array","items":{"$ref":"#/definitions/TransactionResultDto"}}}}}},"/api/chain/blockHeight":{"get":{"tags":["Chain"],"operationId":"GetBlockHeight","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"format":"int64","type":"integer"}}}}},"/api/chain/blockInfo":{"get":{"tags":["Chain"],"operationId":"GetBlockInfo","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"blockHashOrHeight","in":"query","required":false,"type":"string"},{"name":"includeTransactions","in":"query","required":false,"type":"boolean","default":false}],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/BlockDto"}}}}},"/api/chain/transactionPoolStatus":{"get":{"tags":["Chain"],"operationId":"GetTransactionPoolStatus","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/GetTransactionPoolStatusOutput"}}}}},"/api/chain/chainStatus":{"get":{"tags":["Chain"],"operationId":"GetChainStatus","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/ChainStatusDto"}}}}},"/api/chain/blockState":{"get":{"tags":["Chain"],"operationId":"GetBlockState","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"blockHash","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"$ref":"#/definitions/BlockStateDto"}}}}},"/api/net/peer":{"post":{"tags":["Net"],"operationId":"AddPeer","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"address","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"type":"boolean"}}}},"delete":{"tags":["Net"],"operationId":"RemovePeer","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"address","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"type":"boolean"}}}}},"/api/net/peers":{"get":{"tags":["Net"],"operationId":"GetPeers","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"uniqueItems":false,"type":"array","items":{"type":"string"}}}}}},"/api/versionTest/test":{"get":{"tags":["VersionTest"],"operationId":"GetTest","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[],"responses":{"200":{"description":"Success","schema":{"type":"string"}}}},"post":{"tags":["VersionTest"],"operationId":"PostTest","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"test","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"type":"string"}}}},"delete":{"tags":["VersionTest"],"operationId":"DeleteTest","consumes":[],"produces":["text/plain; v=1.0","application/json; v=1.0","text/json; v=1.0","application/x-protobuf; v=1.0"],"parameters":[{"name":"test","in":"query","required":false,"type":"string"}],"responses":{"200":{"description":"Success","schema":{"type":"string"}}}}}},"definitions":{"GetChainInformationOutput":{"type":"object","properties":{"GenesisContractAddress":{"type":"string"},"ChainId":{"type":"string"}}},"BroadcastTransactionOutput":{"type":"object","properties":{"TransactionId":{"type":"string"}}},"TransactionResultDto":{"type":"object","properties":{"TransactionId":{"type":"string"},"Status":{"type":"string"},"Logs":{"uniqueItems":false,"type":"array","items":{"$ref":"#/definitions/LogEventDto"}},"Bloom":{"type":"string"},"BlockNumber":{"format":"int64","type":"integer"},"BlockHash":{"type":"string"},"Transaction":{"$ref":"#/definitions/TransactionDto"},"ReadableReturnValue":{"type":"string"},"Error":{"type":"string"}}},"LogEventDto":{"type":"object","properties":{"Address":{"type":"string"},"Name":{"type":"string"},"Indexed":{"uniqueItems":false,"type":"array","items":{"type":"string"}},"NonIndexed":{"type":"string"}}},"TransactionDto":{"type":"object","properties":{"From":{"type":"string"},"To":{"type":"string"},"RefBlockNumber":{"format":"int64","type":"integer"},"RefBlockPrefix":{"type":"string"},"MethodName":{"type":"string"},"Params":{"type":"string"},"Sigs":{"uniqueItems":false,"type":"array","items":{"type":"string"}}}},"BlockDto":{"type":"object","properties":{"BlockHash":{"type":"string"},"Header":{"$ref":"#/definitions/BlockHeaderDto"},"Body":{"$ref":"#/definitions/BlockBodyDto"}}},"BlockHeaderDto":{"type":"object","properties":{"PreviousBlockHash":{"type":"string"},"MerkleTreeRootOfTransactions":{"type":"string"},"MerkleTreeRootOfWorldState":{"type":"string"},"Extra":{"type":"string"},"Height":{"format":"int64","type":"integer"},"Time":{"format":"date-time","type":"string"},"ChainId":{"type":"string"},"Bloom":{"type":"string"}}},"BlockBodyDto":{"type":"object","properties":{"TransactionsCount":{"format":"int32","type":"integer"},"Transactions":{"uniqueItems":false,"type":"array","items":{"type":"string"}}}},"GetTransactionPoolStatusOutput":{"type":"object","properties":{"Queued":{"format":"int32","type":"integer"}}},"ChainStatusDto":{"type":"object","properties":{"Branches":{"type":"object","additionalProperties":{"format":"int64","type":"integer"}},"NotLinkedBlocks":{"uniqueItems":false,"type":"array","items":{"$ref":"#/definitions/NotLinkedBlockDto"}},"LongestChainHeight":{"format":"int64","type":"integer"},"LongestChainHash":{"type":"string"},"GenesisBlockHash":{"type":"string"},"LastIrreversibleBlockHash":{"type":"string"},"LastIrreversibleBlockHeight":{"format":"int64","type":"integer"},"BestChainHash":{"type":"string"},"BestChainHeight":{"format":"int64","type":"integer"}}},"NotLinkedBlockDto":{"type":"object","properties":{"BlockHash":{"type":"string"},"Height":{"format":"int64","type":"integer"},"PreviousBlockHash":{"type":"string"}}},"BlockStateDto":{"type":"object","properties":{"BlockHash":{"type":"string"},"PreviousHash":{"type":"string"},"BlockHeight":{"format":"int64","type":"integer"},"Changes":{"type":"object","additionalProperties":{"type":"string"}}}}}}
```