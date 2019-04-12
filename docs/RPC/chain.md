Reference of commands exposed in the Chain namespace.

## GetCommands 
This method will return the list of available commands exposed by the nodes RPC endpoint.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetCommands | none       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetCommands",
    "params":{},
    "id": 1
  }
  ```
***response***
  ```
  {
    "result": [
      "BroadcastTransaction",
      "GetTransactionResult",
      ...
    ]
  }
  ```

## GetChainInformation
  This method will return some basic information about the nodes chain.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetChainInformation | none       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetChainInformation",
    "params":{},
    "id": 1
  }
  ```
***response***
```
    "result": {
        "GenesisContractAddress": "61W3AF3Voud7cLY2mejzRuZ4WEN8mrDMioA9kZv3H8taKxF",
        "ChainId": "AELF"
    }
```

  ## Call
  This method is used to call a read-only method on a contract.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| Call | raw tx       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method": "Call",
    "params": {
        "rawTransaction": "xxxxxxxxxxxx"
    },
    "id": 1
  }
  ```
***response***
```
    "result": {
      "Return":xxxxxxxxxxx
    }
```

## BroadcastTransaction
  This method will broadcast a transaction.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| BroadcastTransaction | raw tx       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method": "BroadcastTransaction",
    "params": {
        "rawTransaction": "xxxxxxxxxxxx"
    },
    "id": 1
  }
  ```
***response***
```
    "result": {
      "TransactionId": "ea00df5825e4d262b5445d497469810f24b67fb27b8f38a00d08de53eac94122"
    }
```

  ## BroadcastTransactions
This method will broadcast multiple transactions.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| BroadcastTransactions | raw txs       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method": "BroadcastTransactions",
    "params": {
        "rawTransaction": "xxxxxxxxxxxx"
    },
    "id": 1
  }
  ```
***response***
```
    "result": {
      "Return":xxxxxxxxxxx
    }
```

## GetTransactionResult
This method will return the current status of a transaction.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetTransactionResult | tx id       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method": "GetTransactionResult",
    "params": {
      "transactionId": "4a2df99299f02a69970ec9118f245a905015e31db233180738606ab5d77104d6"
    },
    "id": 1
  }
  ```
***response***
```
    "result": {
        "TransactionId": "4a2df99299f02a69970ec9118f245a905015e31db233180738606ab5d77104d6",
        "Status": "Mined",
        "BlockNumber": "3",
        "Transaction": {
            "From": "4FeP2qLTgZLVFsziS7UDUA4hz2G47WHC8Lsbpx5df6QCCfq",
            "To": "59A6QWHwvwwMVur6kqZWJwsdmpcHX4SHepZQxwUMcJMhfiH",
            "RefBlockNumber": "2",
            "RefBlockPrefix": "y178kg==",
            "MethodName": "PackageOutValue",
            "Params": "CnQKIgogLpYTlMXO4iq4kE2Aa0Hx+URrcNtDr0kfAuxlg1mkKi4SIgogQ6m7mJdR8lqSnQEFOkunB5TgGaqzN3c/eWeX0cMYoPEY+72KrBEgASoiCiDjsMRCmPwcFJr79MiZb7kkJ65B5GSbk0yklZkbeFK4VQ==",
            "Sigs": [
                "wlneUWJMdz1wN6e4ehYMezV3wfbxSftIv4+5jYmCr4loUqMYGyFtSsgZRwYN+liGPBueEYRngIWUTituqqe7HgE="
            ],
            "Type": "DposTransaction"
        }
    }
```

## GetTransactionsResult
Get multiple transaction results.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetTransactionsResult | block hash, offset, num       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method": "GetTransactionResult",
    "params":{
        "blockHash":"4d0ace38e4231cc62c215677f6bd4dcd10d7623aa04a02de2499bcad8a075dbc",
        "offset":0,
        "limit":10
    },
    "id": 1
  }
  ```
***response***
```
    "result": [
        ...,
        {
            "TransactionId": "4a2df99299f02a69970ec9118f245a905015e31db233180738606ab5d77104d6",
            "Status": "Mined",
            "BlockNumber": "3",
            "Transaction": {
                "From": "4FeP2qLTgZLVFsziS7UDUA4hz2G47WHC8Lsbpx5df6QCCfq",
                "To": "59A6QWHwvwwMVur6kqZWJwsdmpcHX4SHepZQxwUMcJMhfiH",
                "RefBlockNumber": "2",
                "RefBlockPrefix": "y178kg==",
                "MethodName": "PackageOutValue",
                "Params": "CnQKIgogLpYTlMXO4iq4kE2Aa0Hx+URrcNtDr0kfAuxlg1mkKi4SIgogQ6m7mJdR8lqSnQEFOkunB5TgGaqzN3c/eWeX0cMYoPEY+72KrBEgASoiCiDjsMRCmPwcFJr79MiZb7kkJ65B5GSbk0yklZkbeFK4VQ==",
                "Sigs": [
                    "wlneUWJMdz1wN6e4ehYMezV3wfbxSftIv4+5jYmCr4loUqMYGyFtSsgZRwYN+liGPBueEYRngIWUTituqqe7HgE="
                ],
                "Type": "DposTransaction"
            }
        }
        ...,
    ]
```

## GetBlockHeight 
Returns the height of the current chain.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetBlockHeight | none       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetBlockHeight",
    "params":{},
    "id": 1
  }
  ```
***response***
  ```
  {
    "result": 220
  }
  ```

  ## GetBlockInfo 
Returns information about a given block. Otionally with the list of its transactions.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetBlockInfo | height, incude txs       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetBlockInfo",
    "params": {
      "blockHeight": "1",
      "includeTransactions":true
    },
    "id": 1
  }
  ```
***response***
  ```
    "result": {
        "BlockHash": "bdcd7e00b48eafb8c5e4f3210ca2c4029a14c4c80ff9ff7fe15b9ad017c26a40",
        "Header": {
            "PreviousBlockHash": "0000000000000000000000000000000000000000000000000000000000000000",
            "MerkleTreeRootOfTransactions": "10f64fd40d9e3e3e621821498877efed71cacc8de98fe08b206f61379b0bd01d",
            "MerkleTreeRootOfWorldState": "ed413f311a0abc2e9e6b45661a204a9dcc650582157f06392efe45b5ec19c38a",
            "Extra": "[ \"CiAwMPUZi1d9RnJBSmgzCE+qghYPWCdK7ZZhG4jn9p77iw==\" ]",
            "Height": "1",
            "Time": "2019-03-25T12:01:43.180025Z",
            "ChainId": "AELF",
            "Bloom": ""
        },
        "Body": {
            "TransactionsCount": 7,
            "Transactions": [
                "d251eb3a17a8072ab8ddaf98270ca57b0830a25dbf6510e834154476f4cf6421",
                "47a48d153e5d047a67ce630575a75a67baa56023f6db3d74567eb9751b894375",
                ...
            ]
        }
    }
  ```

  Reference of commands exposed in the Chain namespace.

## GetTransactionPoolStatus 
This method will return the list of available commands exposed by the nodes RPC endpoint.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetTransactionPoolStatus | none       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetTransactionPoolStatus",
    "params":{},
    "id": 1
  }
  ```
***response***
  ```
  {
    "result": {
        "Queued": 3
    }
  }
  ```

  ## GetFileDescriptorSet 
Returns the protobuf definitions related to a contract.

| Method name | Parameters | Verb    | URL                           | 
| :-----------|:-----------|:--------| :----------------------------:| 
| GetFileDescriptorSet | contract address       | POST    | http://{host}:{port}/chain    |

***request***
  ```
  {
    "jsonrpc":"2.0",
    "method":"GetFileDescriptorSet",
    "params":{},
    "id": 1
  }
  ```
***response***
  ```
  {
    "result": "CpMBCgxjb21tb24ucHJvdG8iGAoHQWRkcmVzcxINCgVWYWx..."
  }
  ```