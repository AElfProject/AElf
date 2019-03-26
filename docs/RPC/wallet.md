
# List accounts

| Method name  | Verb   | URL                           | 
| :------------|:-------| :----------------------------:| 
| ListAccounts | POST   | http://{host}:{port}/wallet   |

* request
  ```
  {
    "jsonrpc":"2.0",
    "method":"ListAccounts",
    "params":{},
    "id":1
  }
  ```
* response
  ```
  {
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
      [
        "ELF_4gm4LrS3MAHYuWGMqEcjGJYgtuGQsJDP8bKbci4zUSXNwgv",
        "ELF_4KBKcTShzurWLvjAGnQS47yMnbLR5X81LU1LgCT2eDTmhcN",
        "ELF_4RGMCjHMjRn4ULvndHNkBk5Wcxd2meg6dF1K4cZUC8GoDat"
      ]
    }
  }
  ```