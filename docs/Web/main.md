# AELF API 1.0

Click to get the [swagger.json](#swagger-json)

<a name="overview"></a>
## Overview

### Version information
*Version* : 1.0




<a name="paths"></a>
## Paths

<a name="getblock"></a>
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


<a name="getblockbyheight"></a>
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


<a name="getblockheight"></a>
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


<a name="getblockstate"></a>
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


<a name="broadcasttransaction"></a>
### Broadcast a transaction
```
POST /api/blockChain/broadcastTransaction
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**rawTransaction**  <br>*optional*|raw transaction|string|


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

* BlockChain


<a name="broadcasttransactions"></a>
### Broadcast multiple transactions
```
POST /api/blockChain/broadcastTransactions
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**rawTransactions**  <br>*optional*|raw transactions|string|


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

* BlockChain


<a name="call"></a>
### Call a read-only method on a contract.
```
POST /api/blockChain/call
```


#### Parameters

|Type|Name|Description|Schema|
|---|---|---|---|
|**Query**|**rawTransaction**  <br>*optional*|raw transaction|string|


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

* BlockChain


<a name="getchainstatus"></a>
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


<a name="getcontractfiledescriptorset"></a>
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


<a name="gettransactionpoolstatus"></a>
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


<a name="gettransactionresult"></a>
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


<a name="gettransactionresults"></a>
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


<a name="addpeer"></a>
### Attempts to add a node to the connected network nodes
```
POST /api/net/peer
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


<a name="removepeer"></a>
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
### Get ip addresses about the connected network nodes
```
GET /api/net/peers
```


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
|**ChainId**  <br>*optional*|string|
|**GenesisBlockHash**  <br>*optional*|string|
|**GenesisContractAddress**  <br>*optional*|string|
|**LastIrreversibleBlockHash**  <br>*optional*|string|
|**LastIrreversibleBlockHeight**  <br>*optional*|integer (int64)|
|**LongestChainHash**  <br>*optional*|string|
|**LongestChainHeight**  <br>*optional*|integer (int64)|
|**NotLinkedBlocks**  <br>*optional*|< [NotLinkedBlockDto](#notlinkedblockdto) > array|


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

You can input it into online Swagger Editor: http://editor.swagger.io/

```json
{
    "swagger": "2.0",
    "info": {
        "version": "1.0",
        "title": "AELF API 1.0"
    },
    "paths": {
        "/api/blockChain/call": {
            "post": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Call a read-only method on a contract.",
                "operationId": "Call",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "rawTransaction",
                        "in": "query",
                        "description": "raw transaction",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "type": "string"
                        }
                    }
                }
            }
        },
        "/api/blockChain/contractFileDescriptorSet": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the protobuf definitions related to a contract",
                "operationId": "GetContractFileDescriptorSet",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "address",
                        "in": "query",
                        "description": "contract address",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "format": "byte",
                            "type": "string"
                        }
                    }
                }
            }
        },
        "/api/blockChain/rawTransaction": {
            "post": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Creates an unsigned serialized transaction",
                "operationId": "CreateRawTransaction",
                "consumes": [
                    "application/json-patch+json; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/*+json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "input",
                        "in": "body",
                        "description": "",
                        "required": false,
                        "schema": {
                            "$ref": "#/definitions/CreateRawTransactionInput"
                        }
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/CreateRawTransactionOutput"
                        }
                    }
                }
            }
        },
        "/api/blockChain/sendRawTransaction": {
            "post": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "send a transaction",
                "operationId": "SendRawTransaction",
                "consumes": [
                    "application/json-patch+json; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/*+json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "input",
                        "in": "body",
                        "description": "",
                        "required": false,
                        "schema": {
                            "$ref": "#/definitions/SendRawTransactionInput"
                        }
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/SendRawTransactionOutput"
                        }
                    }
                }
            }
        },
        "/api/blockChain/broadcastTransaction": {
            "post": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Broadcast a transaction",
                "operationId": "BroadcastTransaction",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "rawTransaction",
                        "in": "query",
                        "description": "raw transaction",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/BroadcastTransactionOutput"
                        }
                    }
                }
            }
        },
        "/api/blockChain/broadcastTransactions": {
            "post": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Broadcast multiple transactions",
                "operationId": "BroadcastTransactions",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "rawTransactions",
                        "in": "query",
                        "description": "raw transactions",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "uniqueItems": false,
                            "type": "array",
                            "items": {
                                "type": "string"
                            }
                        }
                    }
                }
            }
        },
        "/api/blockChain/transactionResult": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the current status of a transaction",
                "operationId": "GetTransactionResult",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "transactionId",
                        "in": "query",
                        "description": "transaction id",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/TransactionResultDto"
                        }
                    }
                }
            }
        },
        "/api/blockChain/transactionResults": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get multiple transaction results.",
                "operationId": "GetTransactionResults",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "blockHash",
                        "in": "query",
                        "description": "block hash",
                        "required": false,
                        "type": "string"
                    },
                    {
                        "name": "offset",
                        "in": "query",
                        "description": "offset",
                        "required": false,
                        "type": "integer",
                        "format": "int32",
                        "default": 0
                    },
                    {
                        "name": "limit",
                        "in": "query",
                        "description": "limit",
                        "required": false,
                        "type": "integer",
                        "format": "int32",
                        "default": 10
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "uniqueItems": false,
                            "type": "array",
                            "items": {
                                "$ref": "#/definitions/TransactionResultDto"
                            }
                        }
                    }
                }
            }
        },
        "/api/blockChain/blockHeight": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the height of the current chain.",
                "operationId": "GetBlockHeight",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "format": "int64",
                            "type": "integer"
                        }
                    }
                }
            }
        },
        "/api/blockChain/block": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get information about a given block by block hash. Otionally with the list of its transactions.",
                "operationId": "GetBlock",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "blockHash",
                        "in": "query",
                        "description": "block hash",
                        "required": false,
                        "type": "string"
                    },
                    {
                        "name": "includeTransactions",
                        "in": "query",
                        "description": "include transactions or not",
                        "required": false,
                        "type": "boolean",
                        "default": false
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/BlockDto"
                        }
                    }
                }
            }
        },
        "/api/blockChain/blockByHeight": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get information about a given block by block height. Otionally with the list of its transactions.",
                "operationId": "GetBlockByHeight",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "blockHeight",
                        "in": "query",
                        "description": "block height",
                        "required": false,
                        "type": "integer",
                        "format": "int64"
                    },
                    {
                        "name": "includeTransactions",
                        "in": "query",
                        "description": "include transactions or not",
                        "required": false,
                        "type": "boolean",
                        "default": false
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/BlockDto"
                        }
                    }
                }
            }
        },
        "/api/blockChain/transactionPoolStatus": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the transaction pool status.",
                "operationId": "GetTransactionPoolStatus",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/GetTransactionPoolStatusOutput"
                        }
                    }
                }
            }
        },
        "/api/blockChain/chainStatus": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the current status of the block chain.",
                "operationId": "GetChainStatus",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/ChainStatusDto"
                        }
                    }
                }
            }
        },
        "/api/blockChain/blockState": {
            "get": {
                "tags": [
                    "BlockChain"
                ],
                "summary": "Get the current state about a given block",
                "operationId": "GetBlockState",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "blockHash",
                        "in": "query",
                        "description": "block hash",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "$ref": "#/definitions/BlockStateDto"
                        }
                    }
                }
            }
        },
        "/api/net/peer": {
            "post": {
                "tags": [
                    "Net"
                ],
                "summary": "Attempts to add a node to the connected network nodes",
                "operationId": "AddPeer",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "address",
                        "in": "query",
                        "description": "ip address",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "type": "boolean"
                        }
                    }
                }
            },
            "delete": {
                "tags": [
                    "Net"
                ],
                "summary": "Attempts to remove a node from the connected network nodes",
                "operationId": "RemovePeer",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [
                    {
                        "name": "address",
                        "in": "query",
                        "description": "ip address",
                        "required": false,
                        "type": "string"
                    }
                ],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "type": "boolean"
                        }
                    }
                }
            }
        },
        "/api/net/peers": {
            "get": {
                "tags": [
                    "Net"
                ],
                "summary": "Get ip addresses about the connected network nodes",
                "operationId": "GetPeers",
                "consumes": [],
                "produces": [
                    "text/plain; v=1.0",
                    "application/json; v=1.0",
                    "text/json; v=1.0",
                    "application/x-protobuf; v=1.0"
                ],
                "parameters": [],
                "responses": {
                    "200": {
                        "description": "Success",
                        "schema": {
                            "uniqueItems": false,
                            "type": "array",
                            "items": {
                                "type": "string"
                            }
                        }
                    }
                }
            }
        }
    },
    "definitions": {
        "CreateRawTransactionInput": {
            "required": [
                "From",
                "To",
                "RefBlockNumber",
                "RefBlockHash",
                "MethodName",
                "Params"
            ],
            "type": "object",
            "properties": {
                "From": {
                    "description": "from address",
                    "type": "string"
                },
                "To": {
                    "description": "to address",
                    "type": "string"
                },
                "RefBlockNumber": {
                    "format": "int64",
                    "description": "refer block height",
                    "type": "integer"
                },
                "RefBlockHash": {
                    "description": "refer block hash",
                    "type": "string"
                },
                "MethodName": {
                    "description": "contract method name",
                    "type": "string"
                },
                "Params": {
                    "description": "contract method parameters",
                    "type": "string"
                }
            }
        },
        "CreateRawTransactionOutput": {
            "type": "object",
            "properties": {
                "RawTransaction": {
                    "type": "string"
                }
            }
        },
        "SendRawTransactionInput": {
            "type": "object",
            "properties": {
                "Transaction": {
                    "description": "raw transaction",
                    "type": "string"
                },
                "Signature": {
                    "description": "signature",
                    "type": "string"
                },
                "ReturnTransaction": {
                    "description": "return transaction detail or not",
                    "type": "boolean"
                }
            }
        },
        "SendRawTransactionOutput": {
            "type": "object",
            "properties": {
                "TransactionId": {
                    "type": "string"
                },
                "Transaction": {
                    "$ref": "#/definitions/TransactionDto"
                }
            }
        },
        "TransactionDto": {
            "type": "object",
            "properties": {
                "From": {
                    "type": "string"
                },
                "To": {
                    "type": "string"
                },
                "RefBlockNumber": {
                    "format": "int64",
                    "type": "integer"
                },
                "RefBlockPrefix": {
                    "type": "string"
                },
                "MethodName": {
                    "type": "string"
                },
                "Params": {
                    "type": "string"
                },
                "Sigs": {
                    "uniqueItems": false,
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                }
            }
        },
        "BroadcastTransactionOutput": {
            "type": "object",
            "properties": {
                "TransactionId": {
                    "type": "string"
                }
            }
        },
        "TransactionResultDto": {
            "type": "object",
            "properties": {
                "TransactionId": {
                    "type": "string"
                },
                "Status": {
                    "type": "string"
                },
                "Logs": {
                    "uniqueItems": false,
                    "type": "array",
                    "items": {
                        "$ref": "#/definitions/LogEventDto"
                    }
                },
                "Bloom": {
                    "type": "string"
                },
                "BlockNumber": {
                    "format": "int64",
                    "type": "integer"
                },
                "BlockHash": {
                    "type": "string"
                },
                "Transaction": {
                    "$ref": "#/definitions/TransactionDto"
                },
                "ReadableReturnValue": {
                    "type": "string"
                },
                "Error": {
                    "type": "string"
                }
            }
        },
        "LogEventDto": {
            "type": "object",
            "properties": {
                "Address": {
                    "type": "string"
                },
                "Name": {
                    "type": "string"
                },
                "Indexed": {
                    "uniqueItems": false,
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                },
                "NonIndexed": {
                    "type": "string"
                }
            }
        },
        "BlockDto": {
            "type": "object",
            "properties": {
                "BlockHash": {
                    "type": "string"
                },
                "Header": {
                    "$ref": "#/definitions/BlockHeaderDto"
                },
                "Body": {
                    "$ref": "#/definitions/BlockBodyDto"
                }
            }
        },
        "BlockHeaderDto": {
            "type": "object",
            "properties": {
                "PreviousBlockHash": {
                    "type": "string"
                },
                "MerkleTreeRootOfTransactions": {
                    "type": "string"
                },
                "MerkleTreeRootOfWorldState": {
                    "type": "string"
                },
                "Extra": {
                    "type": "string"
                },
                "Height": {
                    "format": "int64",
                    "type": "integer"
                },
                "Time": {
                    "format": "date-time",
                    "type": "string"
                },
                "ChainId": {
                    "type": "string"
                },
                "Bloom": {
                    "type": "string"
                }
            }
        },
        "BlockBodyDto": {
            "type": "object",
            "properties": {
                "TransactionsCount": {
                    "format": "int32",
                    "type": "integer"
                },
                "Transactions": {
                    "uniqueItems": false,
                    "type": "array",
                    "items": {
                        "type": "string"
                    }
                }
            }
        },
        "GetTransactionPoolStatusOutput": {
            "type": "object",
            "properties": {
                "Queued": {
                    "format": "int32",
                    "type": "integer"
                }
            }
        },
        "ChainStatusDto": {
            "type": "object",
            "properties": {
                "ChainId": {
                    "type": "string"
                },
                "Branches": {
                    "type": "object",
                    "additionalProperties": {
                        "format": "int64",
                        "type": "integer"
                    }
                },
                "NotLinkedBlocks": {
                    "uniqueItems": false,
                    "type": "array",
                    "items": {
                        "$ref": "#/definitions/NotLinkedBlockDto"
                    }
                },
                "LongestChainHeight": {
                    "format": "int64",
                    "type": "integer"
                },
                "LongestChainHash": {
                    "type": "string"
                },
                "GenesisBlockHash": {
                    "type": "string"
                },
                "GenesisContractAddress": {
                    "type": "string"
                },
                "LastIrreversibleBlockHash": {
                    "type": "string"
                },
                "LastIrreversibleBlockHeight": {
                    "format": "int64",
                    "type": "integer"
                },
                "BestChainHash": {
                    "type": "string"
                },
                "BestChainHeight": {
                    "format": "int64",
                    "type": "integer"
                }
            }
        },
        "NotLinkedBlockDto": {
            "type": "object",
            "properties": {
                "BlockHash": {
                    "type": "string"
                },
                "Height": {
                    "format": "int64",
                    "type": "integer"
                },
                "PreviousBlockHash": {
                    "type": "string"
                }
            }
        },
        "BlockStateDto": {
            "type": "object",
            "properties": {
                "BlockHash": {
                    "type": "string"
                },
                "PreviousHash": {
                    "type": "string"
                },
                "BlockHeight": {
                    "format": "int64",
                    "type": "integer"
                },
                "Changes": {
                    "type": "object",
                    "additionalProperties": {
                        "type": "string"
                    }
                }
            }
        }
    }
}
```
