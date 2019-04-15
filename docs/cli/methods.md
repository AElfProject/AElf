## CLI commands

This page is a reference for the methods that are available on the command line interface (cli). Each example will execute the cli with the **aelf-cli** command, this simply implies that we have aliased the dotnet command to this command.

### Common options

As seen previously, the following options can be specified for all commands, either with the options flag or by setting environment variables:

```bash
-d, --datadir        The directory that contains the files. (AELF_CLI_DATADIR)
-e, --endpoint       The endpoint for the rpc service. (AELF_CLI_ENDPOINT)
-a, --account        The account to be used to interact with the blockchain. (AELF_CLI_ACCOUNT)
-p, --password       The password for unlocking the account.
```

### create - create a new account.

This command will walk you through the process of creating a key-pair. You can decide to save this keypair to the disk. For most scenarios you would reply "yes" because if you don't the key will not be retrievable for later use. If you reply "yes" to saving to the disk, the command will create a .ak file in your **datadir**. This .ak file will contain the encrypted keypair. It's encrypted by a password, so be sure to remember it if you need to use this key again.

```bash 
aelf-cli create 
```

Example:
```bash
aelf-cli create 
Your wallet info is :
Mnemonic    : patch maximum cradle orchard squirrel fantasy permit sphere brick grab conduct normal
Private Key : d53a242ab621fa0b1c88510fcda3604fc40154e5a141d00a8429ca5ca99330a1
Public Key : 04acfaad6d6ced508cde19000fc2feaf761d7619333f4942e5a8d05e58e93a06a81f18875c03b15cb508541312687007cf674225d7686e3524678e770efdb80a54
Address     : 232pnSP16kYHHKhtp9JjEdsCfm58jh3VPprC93oECGGb77z
Saving account info to file? (Y/N): y
Enter a password:
Confirm password:
Account info has been saved to "~/.local/share/aelf/keys/232pnSP16kYHHKhtp9JjEdsCfm58jh3VPprC93oECGGb77z.ak"
```

The command prints both the private and public key as well as the AElf address associated with this key pair. Finally, you'll be able to verify the path where the key-pair is stored.

### deploy - deploy a smart contract.

```bash 
aelf-cli deploy <category> <code>
```

Category    Required. Obsolete. The category of the contract to be deployed.  
Code        Required. The compiled contract code file of the contract to be deployed. This is the path             to the compiled code.  

Example:
```bash
> aelf-cli deploy 0 bin/Debug/netstandard2.0/test-aelf.dll
connect...                                                                                        Deploying Deploying contract...
TransactionId is: 12ad81712e54caa320a1a386196ff7d4bb8ff7d1e2e096c5e71054ce30f2fa90
```

If successful the command will return the ID of the deployement transaction. See get-tx-result for more information.

### send - send a transaction to a contract.

The send command will generate a transaction in order to call a method on the contract at the specified address.

```bash 
aelf-cli send <address> <method> <method-input>
```

address       Required. The address of the contract.  
method        (optional) The particular method of the contract.  
method-input  (optional) The input for the method in json format.

Return the ID of the generated transaction.

Example: send a transaction to the **Transfer** method on the contract at address **4Qj...Eo6**.
```bash
aelf-cli send 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6 Transfer '{amount:"1000",symbol:"ELF",to:"..."}'
connect...
{
  TransactionId: "6d7302651684b5a7ef3a18b204587b6d4b8ee479e7a19ae79bf778a97997be7b"
}
```

Calling with no method name will print the list of methods:

```bash
aelf-cli call 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6
"Method name is required for sending a transaction:
Transfer
...
"
connect...
{
  Value: "Hello world!"
}
```

Call with the method name without parameter will return information about the parameters, like their name and type.

### call - send a transaction to a contract.

The **call** command will generate a transaction in order to call a method on the contract at the specified address. The difference with the **send** is that call will not persist any modifications made to the state in the contract. A transaction created with call will not be included in a block nor broadcast.

```bash 
aelf-cli call <address> <method> <method-input>
```

address       Required. The address of the contract.  
method        (optional) The particular method of the contract.  
method-input  (optional) The input for the method in json format.

Returns the result in JSON format.

Example:
```bash
aelf-cli call 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6 Hello '{}'
connect...
{
  Value: "Hello world!"
}
```

### get-tx-result - get a transaction result.

```bash 
aelf-cli get-tx-result <tx-hash>
```
tx-hash      Required. The tx hash to query.  

Example:
```bash 
aelf-cli get-tx-result ab435790a62abd6a669d002d56771b27bb683a73ce46de0f389ec045e4f3405c
```

```json 
{
  "TransactionId": "ab435790a62abd6a669d002d56771b27bb683a73ce46de0f389ec045e4f3405c",
  "Status": "Mined",
  "Logs": [
    {
      "Address": "61W3AF3Voud7cLY2mejzRuZ4WEN8mrDMioA9kZv3H8taKxF",
      "Topics": [
        "74jhIkU9AGdD4KiVZQ36ybk+DNGNQ8t080m5LJvw11w="
      ]
    }
  ],
  "Bloom": "AAAAAAAAAAA...AAAAAAA==",
  "ReturnValue": "Ch6WzxG27vCo8tf5vmRTkv5T+0sZ81O8yYQQ+HqZhlQ=",
  "BlockNumber": "61",
  "ReadableReturnValue": "\"4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6\"",
  "BlockHash": "2f7d7667c1d13e0f59c26e5d5b839ff53d426a624ce501b4960b99206390c445",
  "Transaction": {
    "From": "2WuYWjzZTs55vxjFnCmKskwwVGsweS8RRX1XeEWJcB96oiz",
    "To": "61W3AF3Voud7cLY2mejzRuZ4WEN8mrDMioA9kZv3H8taKxF",
    "RefBlockNumber": "59",
    "RefBlockPrefix": "oBm5cw==",
    "MethodName": "DeploySmartContract",
    "Params": {
      "code": "TVAAAAAAAAAAAAA...AAAAAAAAA="
    },
    "Sigs": [
      "aZG5lanA3i7DGB2LmZcILNAJQHDOOl+TAi9gxycQqo0DjWxklAe4kKvjy/qHQQa4uc6QgWuCbsv7FfMUfkzNVgA="
    ]
  }
}
```

This shows the transaction result of a smart contract deployement transaction. It return a json with the transaction itself, the return value of the called method (the address of the contract in this case), the status and the hash of the block it was included in (if any).

### get-blk-height - get the block height.

This command get the current height of the best chain.

```bash 
aelf-cli get-blk-height
```
Example:
```bash 
aelf-cli get-blk-height
> 27
```

### get-blk-info - get the block info by block height.

```bash 
aelf-cli get-blk-info <height> <include-txs>
```

height           Required. The height of the block to query.  
include-txs      Whether to include transactions.  

Example:
```bash 
aelf-cli get-blk-info 27 true
```

```json 
{
  "BlockHash": "a81f4f5edab4172d232d1ff9b8536d58d420474cbb30a00e12726abff57c624e",
  "Header": {
    "PreviousBlockHash": "7bce61b6e35e8b1d1abf32991490face1d26d331b736de5c1ca3d2991ee93504",
    "MerkleTreeRootOfTransactions": "ecd9fd9d7890eeb37730728dff845df763668177539d218bccd03e7efdf81da4",
    "MerkleTreeRootOfWorldState": "5276600b71bc47170d43aaa9e0d92599deff35cdc1b21ee88f23b508545fa919",
    "Extra": "[ \"CoIBMDRkZmQ5ODNhMmY2...OWJAAUAB\", \"\", \"CiCkcJf415y5jrOvJ4mDlSR4qNdt4uu/8+QTgqITWgg==\" ]",
    "Height": "27",
    "Time": "2019-04-02T12:40:49.567617Z",
    "ChainId": "AELF",
    "Bloom": "0000000000...000000000000"
  },
  "Body": {
    "TransactionsCount": 1,
    "Transactions": [
      "9462fa843d0e53893dd487101635666bf302734850b7decda74a97f952fe6eed"
    ]
  }
}
```

This has returned information about the block and because the include-txs option was set to true, we can also see the ids of the transaction ids or the transactions that where included in the block.

### console - open an interactive console.

This is a special command that you can use to start an interactive session where you can use javascript to interact with the chain. Type 'exit' to stop the session.

```bash
> dotnet AElf.CLI.dll console
Unlocking account ...
Enter the password: 
Welcome to aelf interactive console. Type exit to terminate the program. Type dir to list objects.
The following objects exist:
_account             _assignToUnderscore  _config                              
_fromString          _getArrayLength      _getOwnProperty                       
_getOwnPropertyNames _repeatedCalls       _requestor                            
_saveAccount         _toString            aelf                                  
Aelf                 chain                console                               
crypto               global               require                               
timer                                                                           
> contract = aelf.chain.contractAt('2UEEa5yiFhuh6JDfTGrbAFqoqzbKkY4Vk9YZDXAdw16wkMw', _account)
> contract.Transfer('2PfhDg55pgKYbnC7UEenbqDyB7DHRTE3G4ZWtyUZQsssq6H', 100)
{                                                                                 
    TransactionId: "c3960811955c7ca60ba290e70ec15d4af16132d5762d2844ee0b867f00d233e1"      
}                                                                               
> aelf.chain.getTxResult('44b095410987db499f657455564bc7f4e0fdfe61cf892df9cf5cc1b27c416333')
{                                                                                 
  BlockNumber: "300",                                                                       
  Bloom: "AAAAAAAAAAAAAAAAA...AAAAAAAAAwAAAAAAAA==",
  Logs: [{
      Address: "2UEEa5yiFhuh6JDfTGrbAFqoqzbKkY4Vk9YZDXAdw16wkMw",
      Data: "CJBO",
      Topics: ["lQq/YqdzlvDN+TAzeSw3dD6sJAK31hfxN9hS6rkOjBw=",         
               "7sOpl2DV7eC6lP3+2ZonpGB2Ve+jvzBjou8EJ3XTMQg=",                        
               "tG1zXS1F39BW0KbnAXgkhAnvKs2JyUpoyLh/NAdv83c="]
     }],
  Status: "Mined",
  TransactionId: "7caaf3f287e91b380c6cdec47461f4ba435b5bd461e8abd93df3cc623f1efb94"                                              
}   
> contract.BalanceOf('ELF_ZGJpxsrpucqBQLKQmUAh27hyD5NNYFLLoBqAjEkZWbdAiDYC6')
"64"                                                                              
> 0x64
100                                                                             
> exit
```