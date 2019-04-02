## Introduction to the CLI

We briefly presented AElfs command line tool in the [getting started](../quickstart.md) section. We discovered that AElf.CLI is the client program used for interacting with a node via RPC calls. You can use it for sending transactions, querying the chains state... It also serves as a wallet program to manage your accounts (keys).

## Build

Navigate to AElfs directory:
```bash
dotnet build AElf.CLI --configuration Release
```
To use the cli just run it with `dotnet` like:
```bash
dotnet AElf.CLI.dll <command> <option1> <option2>
```

The **command** element here refers to any of the available commands, to list them just run the **dll** without providing any arguments: ```dotnet AElf.CLI.dll```. The **options** element refer to the arguments to give to the command. For more about the commands and their options you can refere to the full [command reference](methods.md) of this section.

## Interactive

CLI is built on top of the js library [aelf.js](https://github.com/AElfProject/aelf-sdk.js), so besides using the standard commands directly, you can also use the interactive mode where you can use javascript to interact with the chain. The ```console``` commands will start your session:

```bash
dotnet AElf.CLI.dll console --endpoint=http://localhost:1234 -a 2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
```

## Options and environment variable

Some options are common to all command and can be set by an environment variable. The following three variable can be set: 

1. The `--datadir` option provides the folder that contains the necessary input files (e.g. stored private keys). As this option will be frequently used and may not change from each run, we also provide an environment variable for the default value. It can be set as:
    ```bash
    export AELF_CLI_DATADIR=~/.local/share/aelf
    ```

2. The `--endpoint` option is the rpc endpoint that we are going to connect to. If you are always connecting to a particular endpoint, you can set the default value using environment variable as well:
    ```bash
    export AELF_CLI_ENDPOINT=http://localhost:1234
    ```

3. The `--account` option suggests the account to be used for interacting with the chain. If you are always using the same account, you can set the default value using environment variable.

    ```bash
    export AELF_CLI_ACCOUNT=2jzk2xXHdru6oCGiSyy6mqxTtkWyFbdgBkmrPwNnT5Higm6Tum
    ```

The following option cannot be set with a variable, but is common to many commands: 

```bash
  -p, --password    The passwod for unlocking the account.
```

This option is used for unlocking the account that was previously generated with the ```create``` command. The private key file for the account must be found in `<datadir>/keys` folder. For example, as the value we set in these examples. The private key file `~/.local/share/aelf/keys/2jzk2xXHdru...mrPwNnT5Higm6Tum.ak` must exist.

Note that not all commands require these options. For example, if you are not sending transactions to the chain, `--account` is not required.
As the private keys are encrypted in the `.ak` file, a password is required for unlocking the account. User will be prompted to enter the password for the commands requiring account. However, you can also provide the password by the option `--password`. But we don't recommend to do it this way.

All other options are specific to commands and are explained in the [command reference](methods.md).


