# QuickStart

# Docker quickstart


# Manual build & run the sources

This method is not as straightforward as the docker quickstart but is a lot more flexible. If your aim is to develop some dApps it's better you follow these more advanced ways of launching a node. This section will walk you through configuring, running and interacting with an AElf node.

### Generating the nodes account:
First, if you haven't already done it, clone our [repository](https://github.com/AElfProject/AElf) and stay on the `dev` branch

```bash
git clone https://github.com/AElfProject/AElf.git aelf
```

and build the command line tool:
```bash
dotnet build AElf.CLI --configuration Release
```

Secondly navigate into the **aelf** directory to generate the nodes account (key pair) with AElfs command line tool. For this tutorial we also recommend you alias the cli:

```bash
alias aelf-cli="dotnet AElf.CLI/bin/Release/netcoreapp2.2/AElf.CLI.dll"
```
finally, create the account with the following command:

```bash
aelf-cli create
```
Reply "yes" to saving the account into a file, the commands also asks the user for a password, be sure to remember it for later use.

The output should look like this:

    ```
Your wallet info is :
Mnemonic    : ...
Private Key : 5a5c8d744ff4f96da62e968d5492f9bfd42e7bb2487da69ac55aeabe7d43a9ef
Public Key : 04e768d9d2905df298981f9c32b1e20d5a3df58f20d3bded1e252fbb8be904372d1273d9d485ee46e7da0d94df9cde59744995f9dcdfb74b8053ea4df926ad9ec5
Address     : 5MZJC6u1YWjEUwXugPVeDwXuMrikHUPqqysYtr54tjZmxZN
Saving account info to file? (Y/N): y
...
    ```

Note that a more detailed section about the cli can be found [here]. The last line should give you the path to the default **data directory**.

### Install Redis:
You will now need to install Redis as our node needs a key-value database to store the blockchain data.

### Node configuration:
We have one last step before we can run the node, we have to set up some configuration. Navigate into the **AElf.Launcher** directory:

```bash
cd AElf.Launcher/
```

Open the **appsettings.json** file and edit the following sections:

The miners account:
```json
"Account": 
{
    "NodeAccount": "ELF_5ta1yvi2dFE...THPHcfxMVLrLB",
    "NodeAccountPassword": "pwrd"
},
```
The node account field corresponds to the address, this was printed during the account creation, you also have to enter the password.

```json
"InitialMiners" : [
    "04d8f8fd19cf9e3f7f84e....5cbc30bb7ccb1cc3105e557"
],
```
This is a configuration we use to specify the list of initial miners, for now just configure one, it's the miners public key that was printed during the account creation.

We're now ready to launch the node.

### Launch and test:
Now we build and run the node navigate into the **aelf** directory and build the solution with the following commands:

```bash
cd AElf.Launcher/;dotnet build AElf.Launcher.csproj --configuration Release
dotnet bin/Release/netcoreapp2.2/AElf.Launcher.dll > aelf-logs.logs &
cd ..
```

You now should have a node that's running, to check this run the following command that will query the node for its current block height:

```bash
aelf-cli get-blk-height -e http://127.0.0.1:1728
```




