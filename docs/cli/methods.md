## CLI commands

### Commun commands

```bash
-d, --datadir        The directory that contains the files. Default from env variable: AELF_CLI_DATADIR
-e, --endpoint       The endpoint for the rpc service. Default from env variable: AELF_CLI_ENDPOINT
-a, --account        The account to be used to interact with the blockchain. Default from env variable: AELF_CLI_ACCOUNT
-p, --password       The password for unlocking the account.
```

### console - Open the interactive console.

This is a special command that you can use to start an interactive session in Javascript.

### create - Create a new account.

This command will walk you through the process of creating a key-pair. You can decide to save this keypair to the disk. For most scenarios you would reply "yes" because if you don't the key will not be retrievable later. If you reply "yes" to saving to the disk, the command will create a .ak file in your **datadir**. This .ak file will contain the encrypted keypair. It's encrypted by a password, so be sure to remember it if you need to use this key.

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

### deploy - Deploy a smart contract.

```bash 
aelf-cli create <category> <code>
```

Category    Required. Obsolete. The category of the contract to be deployed.
CodeFile    Required. The compiled contract code file of the contract to be deployed.

### send - Send a transaction to a contract.

```bash 
aelf-cli send <address> <method> <method-input>
```

address       Required. The address of the contract.
method        (optional) The particular method of the contract.
method-input  (optional) The input for the method in json format.

### call - Send a transaction to a contract.

```bash 
aelf-cli call <address> <method> <method-input>
```

address       Required. The address of the contract.
method        (optional) The particular method of the contract.
method-input  (optional) The input for the method in json format.

### get-tx-result - Get a transaction result.

```bash 
aelf-cli get-tx-result <tx-hash>
```
tx-hash      Required. The tx hash to query.

### get-blk-height - Get the block height.

This command get the current height of the best chain.

```bash 
aelf-cli get-blk-height
> 27
```
Example:
```bash 
aelf-cli get-blk-height
> 27
```

### get-blk-info - Get the block info for a block height.

```bash 
aelf-cli get-blk-info <height> <include-txs>
```

height           Required. The height of the block to query.
include-txs      Whether to include transactions.

Example:

```json 
aelf-cli get-blk-info 27 true
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

This has returned information about the block, because the include-txs option was set to true, we can also see the ids of the transaction ids or the transactions that where included in the block.