

We briefly presented aelfs command line tool in the **Getting started** section. We discovered that AElf.CLI is the client program used for interacting with a node via RPC calls. It also serves as a wallet program to manage your accounts (keys). You can build the program by following the guide here.

## Build

```bash
dotnet build AElf.CLI --configuration Release
```

## The command line tool

CLI is built on top of the js library [aelf.js](https://github.com/AElfProject/aelf-sdk.js).

The following commands will help you to explore all the possibilities. To use the tool just run it with `dotnet` like:

```bash
dotnet AElf.CLI.dll
```

If you run the dll without providing any arguments, it lists out all the commands provided by this cli.

```bash
> dotnet AElf.CLI.dll 
AElf 1.0.0
Copyright (C) 2018 AElf.CLI
ERROR(S):
No verb selected.
  create             Create a new account.
  interactive        Open the interactive console.
  deploy             Deploy a smart contract.
  send               Send a transaction to a contract.
  get-tx-result      Get a transaction result.
  get-blk-height     Get the block height.
  get-blk-info       Get the block info for a block height.
  get-merkle-path    Get the merkle path info for an executed transaction.
  help               Display more information on a specific command.
  version            Display version information.
```

You can get the options detail for a specific command. For example, if you need more detailes about the **create** command:
```
> dotnet AElf.CLI.dll help create
AElf 1.0.0
Copyright (C) 2018 AElf.CLI

  -d, --datadir     The directory that contains the files. Default from env 
                    variable: AELF_CLI_DATADIR
  -e, --endpoint    The endpoint for the rpc service. Default from env 
                    variable: AELF_CLI_ENDPOINT
  -a, --account     The account to be used to interact with the blockchain. 
                    Default from env variable: AELF_CLI_ACCOUNT
  -p, --password    The passwod for unlocking the account.
  --help            Display this help screen.
  --version         Display version information.
```

The `create` command doesn't have command specific options besides the four common options shared by all the commands:

1. The `--datadir` option provides the folder that contains the necessary input files (e.g. stored private keys). As this option will be frequently used and may not change from each run, we also provide an environment variable for the default value. It can be set as:
    ```bash
    export AELF_CLI_DATADIR=~/.local/share/aelf
    ```

 1. The `--endpoint` option is the rpc endpoint that we are going to connect to. If you are always connecting to a particular endpoint, you can set the default value using environment variable as well:
    ```bash
    export AELF_CLI_ENDPOINT=http://localhost:1234
    ```

1. The `--account` option suggests the account to be used for interacting with the chain. If you are always using the same account, you can set the default value using environment variable.

    ```bash
    export AELF_CLI_ACCOUNT=ELF_2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
    ```
    Take note that the private key file for the account must be found in `<datadir>/keys` folder. For example, as the value we set in these examples. The private key file `~/.local/share/aelf/keys/ELF_2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum.ak` must exist.

    Please take note that not all commands require these options. For example, if you are not sending transactions to the chain, `--account` is not required.

1. As the private keys are encrypted in the `.ak` file, a password is required for unlocking the account. User will be prompted to enter the password for the commands requiring account. However, you can also provide the password by the option `--password`. But we don't recommend to do it this way.

## Examples
1. Create an account

    ```bash
    > export AELF_CLI_DATADIR=~/.local/share/aelf
    > dotnet AElf.CLI.dll create
    Your wallet info is :
    Mnemonic    : win kit pretty differ tattoo august build volcano critic anxiety corn crazy
    Private Key : 225ae2452c9d7492cd1d6e999c3d4c9d4ff92e6536b62fa8d18410083dfc5bf1
    Public Key : 0402064f20327cc2982252996a6ec6f3855edc480d78c859dbecdd20b016d29c8cb9d6d40b182c96bee8a82767adf5a1ef97ddfcf5d6e5c399909cdf39eb79fbcc
    Address     : kqpozow6sfwnaoWLRzMA87tzr4YVLGjS2FqynaGZW7heMN
    Saving account info to file? (Y/N): y
    Enter a password: 
    Confirm password: 
    Account info has been saved to "~/.local/share/aelf/keys/kqpozow6sfwnaoWLRzMA87tzr4YVLGjS2FqynaGZW7heMN.ak"

    ```

1. Deploy a contract

    ```bash
    > export AELF_CLI_DATADIR=~/.local/share/aelf
    > dotnet AElf.CLI.dll deploy 1 ../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll --endpoint=http://localhost:1234 --account=ELF_2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
    Unlocking account ...
    Enter the password: 
    Your public key is 04aaa35c05a9bb3728b165be9eb6d94207f5ff11347e8eef85e5f05db95e9e771f29a58cb5ba5356ab7f7e01cf6421ccdde19505e896fa7a6325da7438e9119b3f
    connect..
    Deploying contract ...                                       
    TransactionId is: 5af49dfbd53b1607955e5ac1a62efceb5945136b9f8cfe9d2f7b3352a1740482       
    ```

1. Send a transaction

    ```bash
    > dotnet AElf.CLI.dll send 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6 Create '{symbol: "ABCDE", tokenName: "ABCDE Token", totalSupply: "10000000", issuer: "52Sgab5SjwzbFBVp6LgyTHk32hLkg56AAEGxmkDM31BcvWD",lockWhiteList:"3q4pZwip6UkBDk9Dkbrjxge7keufXgBm2PDsGk3G2cbWdpe"}' --endpoint=http://localhost:1234 --account=2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
    Unlocking account ...
    Enter the password: 
    {                   
        TransactionId: "cdb31bb11a2730db3c765941a4260ddeeeed8c38d39f33f7f16cc3dda820a304"
    } 
    ```
        If you don't input the params, the cli will prompt you for a field type.

    ```
     > dotnet AElf.CLI.dll send 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6 Create -a 2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum -p 123
     Unlocking account ...
     Your public key is 04a44091bb29de6252bd7f7d16519ec9ee76b0b3b1fd5339155afd1937879352494ba7cca47f5954f449976f319e1618fbc7fd88e060a51ad3becee886578512da
    {
      fields: {
        decimals: {
          id: 4,
          type: "sint32"
        },
        isBurnable: {
          id: 6,
          type: "bool"
        },
        issuer: {
          id: 5,
          type: ".Address"
        }, 
        lockWhiteList: {
          id: 7,
          rule: "repeated",
          type: ".Address"
        },
        symbol: {
          id: 1,
          type: "string"
        },
        tokenName: {
          id: 2,
          type: "string"
        },
        totalSupply: {
          id: 3,
          type: "sint64"
        }
     }
   }
      ```  
                                       
1. Get transaction result (Note: `--account` is not required)
```
    > dotnet AElf.CLI.dll get-tx-result 40e07c22ee3ea5e064ebe55f620d33b43d93fbddf2e125db5097c92dbeaa0087 -e http://localhost:1234 
  {
    "TransactionId": "40e07c22ee3ea5e064ebe55f620d33b43d93fbddf2e125db5097c92dbeaa0087",
    "Status": "Mined",
    "BlockNumber": "322",
    "ReadableReturnValue": "{ }",
    "BlockHash": "ef8cba07f4262096384e1c079aad13a7a7b5069c9a256eb84f4881a201047547",
    "Transaction": {
      "From": "52Sgab5SjwzbFBVp6LgyTHk32hLkg56AAEGxmkDM31BcvWD",
      "To": "4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6",
      "RefBlockNumber": "320",
      "RefBlockPrefix": "pu6SgQ==",
      "MethodName": "Create",
      "Params": {
        "symbol": "ABCDE",
        "tokenName": "ABCDE Token",
        "totalSupply": "10000000",
        "decimals": 2,`
        "issuer": "52Sgab5SjwzbFBVp6LgyTHk32hLkg56AAEGxmkDM31BcvWD",
        "isBurnable": true
      },
      "Sigs": [
        "h2Io6WF8JwbubLmG4+fpcryCszXd26yKdneUiXZh9RhTIZlrCESZK0tfIJH2tbyvP9vDJvy07kyDRJi6B/PxDgA="
      ]
    }
  }
   ```
1. Call

```
dotnet AElf.CLI.dll call 4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc GetBalance '{symbol:"ELF",owner:"65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9"}' -a QL8AmZ41zYMrTSiJBbPmmEc933h6cHzAZBXcgARtXuLoAT -p 123
Unlocking account ...
Your public key is
049fcac3911a9fcde247f2c7a9513f6d4e28a719f35ef0ea392881a6359dbf7022ce98f828b46be13addf5d53220108cc2f4ace8cb44dd85c6573e725e6d079c9e
{
balance: "10000000",
owner: "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9",
symbol: "ELF"
}
```


1. Get block height

    ```bash
    > dotnet AElf.CLI.dll get-blk-height  --endpoint=http://localhost:1234
      100

    ```

1. Get block info

    ```bash
    > dotnet AElf.CLI.dll get-blk-info 3934 true --endpoint=http://localhost:1234
    {
      "Blockhash": "d4efe5ad8f4fbaef0799b6bf6f055831ff9bb4ce44a290dcc9d38b074c9a461d",
      "Header": {
        "PreviousBlockHash": "95ea4865bfcb9d9e0954c05e2155b2b78aade522bca58ca80cda4de3aa2b283f",
        "MerkleTreeRootOfTransactions": "26a6f74220079ae15033ae75c2ddc4fc10d6fcb7cea2690f137396f9ee054a54",
        "MerkleTreeRootOfWorldState": "c4fa93e2db994ea82c263b7dd4a32acc411bad4a08baa03708c2babb9e883db9",
        "SideChainTransactionsRoot": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        "Height": "3934",
        "Time": "2018-12-10T07:22:06.229809Z"
        "ChainId": "AELF",
        "Bloom": ""
       },
      "Body": {
        "TransactionsCount": 2,
        "Transactions": [
          "ef8add5e09cef123e4f9422944b907d6e5403e05cc17650380ac027a3aaaf372",
          "04f2263b302e39a0588256b92c273c945c306441b5781f2a5dc6bf5bd2857ead"
         ]
       }
    }
    ```

1. Get merkle path

    ```bash
    > dotnet AElf.CLI.dll get-merkle-path ef8add5e09cef123e4f9422944b907d6e5403e05cc17650380ac027a3aaaf372 --endpoint=http://localhost:1234
    {
        "merkle_path": "0a220a2004f2263b302e39a0588256b92c273c945c306441b5781f2a5dc6bf5bd2857ead",
        "parent_height": 0
    }
    ```

## Interactive mode

Besides using the standard commands directly, you can also use `interactive` mode where you can use javascript to interact with the chain.

```bash
> dotnet AElf.CLI.dll console --endpoint=http://localhost:1234 --account=ELF_2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
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
  Bloom: "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAgAAAAAAAAAAAAAAQAAAAAAAACAAAAAIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwAAAAAAAA==",
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