## aelf-command

### Common options

* `datadir`: The directory that contains `aelf-command` files, such as `encrypted account info keyStore` files. Default to be `{home}/.local/share/aelf`
* `endpoint`: The endpoint for the RPC service.
* `account`: The account to be used to interact with the blockchain `endpoint`.
* `password`: The password for unlocking the given `account`.

You can specified options above in several ways, and the priority is in the order of low to high.

1. `export` variables in shell.
```bash
# This is datadir
export AELF_CLI_DATADIR=/Users/your/.local/share/aelf
# This is endpoint 
export AELF_CLI_ENDPOINT=http://127.0.0.1:8000
# This is account
export AELF_CLI_ACCOUNT=2Ue31YTuB5Szy7c...gtGi5uMQBYarYUR5oGin1sys6H
```

2. `aelf-command` global `.aelfrc` config file

The global config file is stored in the `<datadir>/.aelfrc` file, you can read the config file, but better not modify it by yourself.

Modify this config file by `aelf-command config`.

  - `set`: set and save config in the file, remember just set `datadir`, `endpoint`, `account`, `password` four keys.
    ```bash
    aelf-command config set endpoint http://127.0.0.1:8000
    aelf-command config -h

    Usage: aelf-command config [options] <flag> [key] [value]
    
    get, set, delete or list aelf-command config
    
    Options:
      -h, --help  output usage information
    
    Examples:
    
    aelf-command config get <key>
    aelf-command config set <key> <value>
    aelf-command config delete <key>
    aelf-command config list
    ``` 
  - `get`: get the value of given `key` from global `.aelfrc` file
    ```bash
    aelf-command config get endpoint
    http://127.0.0.1:8000
    ```
  - `delete`: delete the `<key, value>` from global `.aelfrc` file by a given key
    ```bash
    aelf-command config delete endpoint
    ```
  - `list`: get the list of all configs stored in global `.aelfrc` file
    ```bash
    aelf-command config list
    endpoint=http://127.0.0.1:8000
    ```

Remember `config` command only can be used to modify the global `.aelfrc` file for now, more usages such as modify working directory will be implemented in later.

3. `aelf-command` working directory `.aelfrc` file

The current working directory of `aelf-command` can have a file named `.aelfrc` and store configs, the format of this file is like global `.aelfrc` file:

```text
endpoint http://127.0.0.1:8000
password yourpassword
```
each line is `<key, value>` config and a whitespace is needed to separate them.

4. `aelf-command` options.

You can give common options by passing them in CLI parameters.

```bash
aelf-command console -a sadaf -p password -e http://127.0.0.1:8000
```

Notice the priority, the options given in higher priority will overwrite the lower priority.

### create - create a new account

This command will create a new account.

```bash
Usage: aelf-command create [options] [save-to-file]

create a new account

Options:
  -c, --cipher [cipher]  Which cipher algorithm to use, default to be aes-256-cbc
  -h, --help             output usage information

Examples:

aelf-command create <save-to-file>
aelf-command create
```

Example:

* Specify the cipher way to encrypt account info by passing option `-c [cipher]`, such as:
```shell script
aelf-command create -c aes-128-cbc
```

### load - load an account by a given `private key` or `mnemonic`

This command allow you load an account from backup.

```bash
# load from mnemonic
aelf-command load 'great mushroom loan crisp ... door juice embrace'
# load from private key
aelf-command load 'e038eea7e151eb451ba2901f7...b08ba5b76d8f288'
# load from prompting
aelf-command load
? Enter a private key or mnemonic › e038eea7e151eb451ba2901f7...b08ba5b76d8f288
...
```

### deploy - deploy a smart contract

Deploy a smart contract to the chain

```bash
aelf-command deploy
✔ Enter a valid wallet address, if you don't have, create one by aelf-command create … 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H
✔ Enter the password you typed when creating a wallet … ********
✔ Enter the category of the contract to be deployed … 0
? Enter the relative or absolute path of contract code › /Users/home/home
```

### send - send a transaction

```bash
aelf-command send
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
✔ Enter a valid wallet address, if you don't have, create one by aelf-command create … D3vSjRYL8MpeRpvUDy85ktXijnBe2tHn8NTACsggUVteQCNGP
✔ Enter the password you typed when creating a wallet … ********
✔ Enter contract name (System contracts only) or the address of contract … AElf.ContractNames.Token
✔ Succeed
✔ Pick up a contract method › GetTokenInfo
✔ Enter the method params in JSON string format … {"symbol":"ELF"}
✔ Succeed!

Result:
{
  "TransactionId": "7b620a49ee9666c0c381fdb33f94bd31e1b5eb0fdffa081463c3954e9f734a02"
}
✔ Succeed!
```

```bash
aelf-command send AElf.ContractNames.Token GetTokenInfo '{"symbol":"ELF"}'
aelf-command send WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM GetTokenInfo '{"symbol":"ELF"}'
```

### call - call a read-only method on a contract

```bash
aelf-command call
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
✔ Enter a valid wallet address, if you don't have, create one by aelf-command create … D3vSjRYL8MpeRpvUDy85ktXijnBe2tHn8NTACsggUVteQCNGP
✔ Enter the password you typed when creating a wallet … ********
✔ Enter contract name (System contracts only) or the address of contract … AElf.ContractNames.Token
✔ Succeed
✔ Pick up a contract method › GetTokenInfo
✔ Enter the method params in JSON string format … {"symbol":"ELF"}
✔ Succeed!

Result:
{
  "symbol": "ELF",
  "tokenName": "elf token",
  "supply": "1000000000",
  "totalSupply": "1000000000",
  "decimals": 2,
  "issuer": "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8",
  "isBurnable": true
}
✔ Succeed!
```

```bash
aelf-command call AElf.ContractNames.Token GetTokenInfo '{"symbol":"ELF"}'
aelf-command call WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM GetTokenInfo '{"symbol":"ELF"}'
```

### get-tx-result - get a transaction result

```bash
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
✔ Enter a valid transaction hash in hex format … 7b620a49ee9666c0c381fdb33f94bd31e1b5eb0fdffa081463c3954e9f734a02
✔ Succeed!
{ TransactionId:
   '7b620a49ee9666c0c381fdb33f94bd31e1b5eb0fdffa081463c3954e9f734a02',
  Status: 'MINED',
  Logs: null,
  Bloom:
   'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==',
  BlockNumber: 7900508,
  BlockHash:
   'a317c5ecf4a22a481f88ab08b8214a8e8c24da76115d9ddcef4afc9531d01b4b',
  Transaction:
   { From: 'D3vSjRYL8MpeRpvUDy85ktXijnBe2tHn8NTACsggUVteQCNGP',
     To: 'WnV9Gv3gioSh3Vgaw8SSB96nV8fWUNxuVozCf6Y14e7RXyGaM',
     RefBlockNumber: 7900503,
     RefBlockPrefix: 'Q6WLSQ==',
     MethodName: 'GetTokenInfo',
     Params: '{ "symbol": "ELF" }',
     Signature:
      'JtSpWbMX13tiJD0klMSJQyPBa0aRNFY4hTh3hltdWqhBpv4IRTbjjZfQj39lbBSCOy68vnLg6rUerEcyCsqwfgE=' },
  ReadableReturnValue:
   '{ "symbol": "ELF", "tokenName": "elf token", "supply": "1000000000", "totalSupply": "1000000000", "decimals": 2, "issuer": "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8", "isBurnable": true }',
  Error: null }
```

### get-blk-height - get the block height

```bash
aelf-command get-blk-height
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
> 7902091
```

### get-blk-info - get the block info by a block height

```bash
aelf-command get-blk-info
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
✔ Enter a valid height … 123
✔ Include transactions whether or not … no / yes
{ BlockHash:
   '6034db3e02e283d3b81a4528442988d28997d3828f87cca1a89457b294517372',
  Header:
   { PreviousBlockHash:
      '9d6bcc588c0bc10942899e7ec4536665c86f23286029ed45287babf22c582f5a',
     MerkleTreeRootOfTransactions:
      '7ceb349715787ececa647ad48576467d294de6dcc44d14e19f60c4a91a7a9536',
     MerkleTreeRootOfWorldState:
      'b529e2775283edc39cd4e3f685616085b18bd5521a87ea7904ad99cd2dc50910',
     Extra:
      '[ "CkEEJT3FEw+k9cuqv7uruq1fEwQwEjKtYxbXK86wUGrAOH7BgCVkMendLkQZmpEpMgzcz+JXnaVpWtFt3AJcGmGycxL+DggIEvIDCoIBMDQyNTNkYzUxMzBmYTRmNWNiYWFiZmJiYWJiYWFkNWYxMzA0MzAxMjMyYWQ2MzE2ZDcyYmNlYjA1MDZhYzAzODdlYzE4MDI1NjQzMWU5ZGQyZTQ0MTk5YTkxMjkzMjBjZGNjZmUyNTc5ZGE1Njk1YWQxNmRkYzAyNWMxYTYxYjI3MxLqAggCIiIKIOAP2QU8UpM4u9Y3OxdKdI5Ujm3DSyQ4JaRNf7q5ka5mKiIKIH5yNJs87wb/AkWcIrCxvCX/Te3fGHVXFxE8xsnfT1HtMgwIoJro6AUQjOa1pQE4TkqCATA0MjUzZGM1MTMwZmE0ZjVjYmFhYmZiYmFiYmFhZDVmMTMwNDMwMTIzMmFkNjMxNmQ3MmJjZWIwNTA2YWMwMzg3ZWMxODAyNTY0MzFlOWRkMmU0NDE5OWE5MTI5MzIwY2RjY2ZlMjU3OWRhNTY5NWFkMTZkZGMwMjVjMWE2MWIyNzNiIgogHY83adsNje+EtL0lLEte8KfT6X/836zXZTbntbqyjgtoBHAEegwIoJro6AUQzOybpgF6DAigmujoBRCk8MG1AnoLCKGa6OgFEOCvuBF6CwihmujoBRCg/JhzegwIoZro6AUQ9Lml1wF6DAihmujoBRDYyOO7AnoMCKGa6OgFEKy+ip8DkAEOEp8CCoIBMDQ4MWMyOWZmYzVlZjI5NjdlMjViYTJiMDk0NGVmODQzMDk0YmZlOTU0NWFhZGFjMGQ3Nzk3MWM2OTFjZTgyMGQxYjNlYzQxZjNjMDllNDZjNmQxMjM2NzA5ZTE1ZTEyY2U5N2FhZGNjYTBmZGU4NDY2M2M3OTg0OWZiOGYwM2RkMhKXAQgEMgwIpJro6AUQjOa1pQE4IkqCATA0ODFjMjlmZmM1ZWYyOTY3ZTI1YmEyYjA5NDRlZjg0MzA5NGJmZTk1NDVhYWRhYzBkNzc5NzFjNjkxY2U4MjBkMWIzZWM0MWYzYzA5ZTQ2YzZkMTIzNjcwOWUxNWUxMmNlOTdhYWRjY2EwZmRlODQ2NjNjNzk4NDlmYjhmMDNkZDISnwIKggEwNDFiZTQwMzc0NjNjNTdjNWY1MjgzNTBhNjc3ZmRkZmEzMzcxOWVlZjU5NDMwNDY5ZTlmODdkY2IyN2Y0YTQ1NjY0OTI4NmZhNzIxYzljOWVjZDMxMmY0YjdlZDBmZGE4OTJmZTNlZDExZWFjYTBmMzcxOTBkMjAzYTczYTA2YjFmEpcBCAYyDAiomujoBRCM5rWlATgySoIBMDQxYmU0MDM3NDYzYzU3YzVmNTI4MzUwYTY3N2ZkZGZhMzM3MTllZWY1OTQzMDQ2OWU5Zjg3ZGNiMjdmNGE0NTY2NDkyODZmYTcyMWM5YzllY2QzMTJmNGI3ZWQwZmRhODkyZmUzZWQxMWVhY2EwZjM3MTkwZDIwM2E3M2EwNmIxZhKfAgqCATA0OTMzZmYzNDRhNjAxMTdmYzRmYmRmMDU2ODk5YTk0NDllNjE1MzA0M2QxYzE5MWU4NzlkNjlkYzEzZmIyMzM2NWJmNTQxZWM1NTU5MWE2MTQ3YmM1Y2M3ZjUzMjQ0OTY2ZGE5NzA2ZWZmNzZiY2Y2ZjViY2EyOTYzNmVmODNkYzYSlwEICjIMCLCa6OgFEIzmtaUBOCJKggEwNDkzM2ZmMzQ0YTYwMTE3ZmM0ZmJkZjA1Njg5OWE5NDQ5ZTYxNTMwNDNkMWMxOTFlODc5ZDY5ZGMxM2ZiMjMzNjViZjU0MWVjNTU1OTFhNjE0N2JjNWNjN2Y1MzI0NDk2NmRhOTcwNmVmZjc2YmNmNmY1YmNhMjk2MzZlZjgzZGM2EpUDCoIBMDRiNmMwNzcxMWJjMzBjZGY5OGM5ZjA4MWU3MDU5MWY5OGYyYmE3ZmY5NzFlNWExNDZkNDcwMDlhNzU0ZGFjY2ViNDY4MTNmOTJiYzgyYzcwMDk3MWFhOTM5NDVmNzI2YTk2ODY0YTJhYTM2ZGE0MDMwZjA5N2Y4MDZiNWFiZWNhNBKNAggIEAEyDAismujoBRCM5rWlATgwQAJKggEwNGI2YzA3NzExYmMzMGNkZjk4YzlmMDgxZTcwNTkxZjk4ZjJiYTdmZjk3MWU1YTE0NmQ0NzAwOWE3NTRkYWNjZWI0NjgxM2Y5MmJjODJjNzAwOTcxYWE5Mzk0NWY3MjZhOTY4NjRhMmFhMzZkYTQwMzBmMDk3ZjgwNmI1YWJlY2E0egwInJro6AUQjOa1pQF6DAicmujoBRCkz+mjAnoMCJya6OgFEIj8yfECegwInJro6AUQ7KiH0wN6CwidmujoBRCko6hXegwInZro6AUQ6LTNugF6DAidmujoBRCY4NObAnoMCJ2a6OgFEMzWv+oCkAEQIFg6ggEwNGI2YzA3NzExYmMzMGNkZjk4YzlmMDgxZTcwNTkxZjk4ZjJiYTdmZjk3MWU1YTE0NmQ0NzAwOWE3NTRkYWNjZWI0NjgxM2Y5MmJjODJjNzAwOTcxYWE5Mzk0NWY3MjZhOTY4NjRhMmFhMzZkYTQwMzBmMDk3ZjgwNmI1YWJlY2E0QAIYBQ==", "" ]',
     Height: 123,
     Time: '2019-07-01T13:39:45.8704899Z',
     ChainId: 'AELF',
     Bloom:
      '00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000',
     SignerPubkey:
      '04253dc5130fa4f5cbaabfbbabbaad5f1304301232ad6316d72bceb0506ac0387ec180256431e9dd2e44199a9129320cdccfe2579da5695ad16ddc025c1a61b273' },
  Body:
   { TransactionsCount: 1,
     Transactions:
      [ 'a365a682caf3b586cbd167b81b167979057246a726c7282530554984ec042625' ] } }
```

### console - open an interactive console

```bash
aelf-command console
✔ Enter the the URI of an AElf node … http://13.231.179.27:8000
✔ Enter a valid wallet address, if you don't have, create one by aelf-command create … 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H
✔ Enter the password you typed when creating a wallet … ********
✔ Succeed!
Welcome to aelf interactive console. Ctrl + C to terminate the program. Double tap Tab to list objects

   ╔═══════════════════════════════════════════════════════════╗
   ║                                                           ║
   ║   NAME       | DESCRIPTION                                ║
   ║   AElf       | imported from aelf-sdk                     ║
   ║   aelf       | the instance of an aelf-sdk, connect to    ║
   ║              | http://13.231.179.27:8000                  ║
   ║   _account   | the instance of an AElf wallet, address    ║
   ║              | is                                         ║
   ║              | 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR…   ║
   ║              | 5oGin1sys6H                                ║
   ║                                                           ║
   ╚═══════════════════════════════════════════════════════════╝
```

