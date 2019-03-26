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
    "jsonrpc": "2.0",
    "id": 1,
    "result": [
      "BroadcastTransaction",
      "GetTransactionResult",
      ...
    ]
  }
  ```