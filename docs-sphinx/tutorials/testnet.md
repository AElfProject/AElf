How to join the testnet
=======================

There's two ways to run a AElf node: you can either use Docker
(recommended method) or run the binaries available on Github. Before you
jump into the guides and tutorials you'll need to install the following
tools and frameworks. For most of these dependencies we provide
ready-to-use command line instructions. In case of problems or if you
have more complex needs, we provide more information in the [Environment
setup](../../getting-started/development-environment/install.md) section.

Summary of the steps to set up a node:

1.  Execute the snapshot download script and load the snapshot into the
    database.
2.  Download our template setting files and docker run script.
3.  Modify the appsettings according to your needs.
4.  Run and check the node.

Hardware suggestion: for the AElf testnet we use the following Amazon
configuration: c5.large instance with 2 vCPUs, 4GiB RAM and a 200GiB
hard drive for each node we run. We recommend using something similar
per node that you want to run (one for the mainchain node and one per
side chain node).

**Note**: any server you use to run a node should be time synced via
NTP. Failing to do this will prevent your node from syncing.

Setup the database
------------------

We currently support two key-value databases to store our nodes data:
Redis and SSDB, but for the testnet we only provide snapshots for SSDB.
We will configure two SSDB instances, one for chain database and one for
the state database (run these on different machines for better
performances).

### Import the snapshot data

After you've finished setting up the database, download the latest
snapshots. The following gives you the template for the download URL,but
you have to specify the snapshot date. We recommend you get the latest.

Restore the chain database from snapshot:

``` bash
>> mkdir snapshot
>> cd snapshot

## fetch the snapshot download script
>> curl -O -s https://aelf-node.s3-ap-southeast-1.amazonaws.com/snapshot/testnet/download-mainchain-db.sh

## execute the script, you can optionally specify a date by appending “yyyymmdd” as parameter
>> sh download-mainchain-db.sh

## chain database: decompress and load the chain database snapshot
>> tar xvzf aelf-testnet-mainchain-chaindb-*.tar.gz
>> stop your chain database instance (ssdb server)
>> cp -r aelf-testnet-mainchain-chaindb-*/* /path/to/install/chaindb/ssdb/var/
>> start your chain database instance
>> enter ssdb console (ssdb-cli) use the "info" command to confirm that the data has been imported)

## state database : decompress and load the state database
>> tar xvzf aelf-testnet-mainchain-statedb-*.tar.gz
>> stop your state database instance (ssdb server)
>> cp -r aelf-testnet-mainchain-statedb-*/* /path/to/install/statedb/ssdb/var/
>> start your state database instance
>> enter ssdb console (ssdb-cli) use the "info" command to confirm that the data has been imported)
```

Node configuration
------------------

### Generating the nodes account

This section explains how to generate an account for the node. First you
need to install the aelf-command npm package. Open a terminal and enter
the following command to install aelf-command:

``` bash
>> npm i -g aelf-command
```

After installing the package, you can use the following command to
create an account/key-pair:

``` bash
>> aelf-command create
```

The command prompts for a password, enter it and don't forget it. The
output of the command should look something like this:

    AElf [Info]: Your wallet info is :
    AElf [Info]: Mnemonic            : term jar tourist monitor melody tourist catch sad ankle disagree great adult
    AElf [Info]: Private Key         : 34192c729751bd6ac0a5f18926d74255112464b471aec499064d5d1e5b8ff3ce
    AElf [Info]: Public Key          : 04904e51a944ab13b031cb4fead8caa6c027b09661dc5550ee258ef5c5e78d949b1082636dc8e27f20bc427b25b99a1cadac483fae35dd6410f347096d65c80402
    AElf [Info]: Address             : 29KM437eJRRuTfvhsB8QAsyVvi8mmyN9Wqqame6TsJhrqXbeWd
    ? Save account info into a file? Yes
    ? Enter a password: *********
    ? Confirm password: *********
    ✔ Account info has been saved to "/usr/local/share/aelf/keys/29KM437eJRRuTfvhsB8QAsyVvi8mmyN9Wqqame6TsJhrqXbeWd.json"

In the next steps of the tutorial you will need the Public Key and the
Address for the account you just created. You'll notice the last line of
the commands output will show you the path to the newly created key. The
aelf directory is the data directory (datadir) and this is where the
node will read the keys from.

Note that a more detailed section about the cli can be found [command line interface](../../reference/cli/introduction.md).

### Prepare node configuration

``` bash
## download the settings template and docker script
>> cd /tmp/ && wget https://github.com/AElfProject/AElf/releases/download/v1.0.0-rc1/aelf-testnet-mainchain.zip
>> unzip aelf-testnet-mainchain.zip
>> mv aelf-testnet-mainchain /opt/aelf-node
```

Update the appsetting.json file with your account. This will require the
information printed during the creation of the account. Open the
appsettings.json file and edit the following sections.

The account/key-pair associated with the node we are going to run:

``` json
{
    "Account": {
        "NodeAccount": "2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H",
        "NodeAccountPassword": "********"
    }
}
```

You also have to configure the database connection strings (port/db
number):

``` json
{
    "ConnectionStrings": {
        "BlockchainDb": "redis://your chain database server ip address:port",
        "StateDb": "redis://your state database server ip address:port"
    },
}
```

If you use docker to run the node and it is on
the same server as the database, please do not use 127.0.0.1 as the
database monitoring ip. 

Next add the testnet mainchain nodes as peer (bootnode peers):

``` json
{
    "Network": {
        "BootNodes": [
            "xxx.xxxx.xxx.xxx:6800",
            "..."
        ],
        "ListeningPort": 6800
    }
}
```

Note: if your infrastructure is behind a firewall you need to open the
P2P listening port of the node. You also need to configure your
listening ip and port for the side chain connections in `appsettings.MainChain.TestNet.json`:

``` json
{
    "CrossChain": {
        "Grpc": {
            "LocalServerPort": 5000,
        }
    },
}
```

Running a full node with Docker
-------------------------------

To run the node with Docker, enter the following commands:

``` bash
## pull AElf’s image and navigate to the template folder to execute the start script
>> docker pull aelf/node:testnet-v1.0.0
>> cd /opt/aelf-node
>> sh aelf-node.sh start aelf/node:testnet-v1.0.0
```

to stop the node you can run:

``` bash
>> sh aelf-node.sh stop
```

Running a full node with the binary release
-------------------------------------------

Most of AElf is developed with dotnet core, so to run the binaries you
will need to download and install the .NET Core SDK before you start:
[Download .NET Core
6.0](https://dotnet.microsoft.com/download/dotnet-core/6.0). For now
AElf depends on version 6.0 of the SDK, on the provided link find the
download for your platform, and install it.

Get the latest release with the following commands:

``` bash
>> cd /tmp/ && wget https://github.com/AElfProject/AElf/releases/download/v1.0.0-rc1/aelf.zip
>> unzip aelf.zip
>> mv aelf /opt/aelf-node/
```

Enter the configuration folder and run the node:

``` bash
>> cd /opt/aelf-node
>> dotnet aelf/AElf.Launcher.dll
```

Running a full node with the source
-----------------------------------

The most convenient way is to directly use docker or the binary
packages, but if you want you can compile from source code. First make
sure the code version is consistent (current is release AELF
v1.0.0), and secondly make sure to compile on a Ubuntu Linux
machine (we recommend Ubuntu 18.04.2 LTS) and have dotnet core SDK
version 6.0 installed. This is because different platforms or compilers
will cause the dll hashes to be inconsistent with the current chain.

Check the node
--------------

You now should have a node that's running, to check this run the
following command that will query the node for its current block height:

``` bash
aelf-command get-blk-height -e http://your node ip address:port
```

Run side-chains
---------------

This section explains how to set up a side-chain node, you will have to
repeat these steps for all side chains (currently only one is running):

1.  Fetch the appsettings and the docker run script.
2.  Download and restore the snapshot data with the URLs provided below
    (steps are the same as in A - Setup the database).
3.  Run the side-chain node.

Running a side chain is very much like running a mainchain node, only
configuration will change. Here you can find the instructions for
sidechain1:

``` bash
>> cd /tmp/ && wget https://github.com/AElfProject/AElf/releases/download/v1.0.0-rc1/aelf-testnet-sidechain1.zip
>> unzip aelf-testnet-sidechain1.zip
>> mv aelf-testnet-sidechain1 /opt/aelf-node
```

In order for a sidechain to connect to a mainchain node you need to
modify the `appsettings.SideChain.TestNet.json` with your node information.

``` json
{
    "CrossChain": {
        "Grpc": {
            "ParentChainServerPort": 5000,
            "ParentChainServerIp": "your mainchain ip address",
            "ListeningPort": 5001,
        },
        "ParentChainId": "AELF"
    }
}
```

Here you can find the snapshot data for the only current side-chain
running, optionally you can specify the date, but we recommend you get
the latest:

    >> curl -O -s https://aelf-node.s3-ap-southeast-1.amazonaws.com/snapshot/testnet/download-sidechain1-db.sh 

Here you can find the list of templates folders (appsettings and docker
run script) for the side-chain:

    wget https://github.com/AElfProject/AElf/releases/download/v1.0.0-rc1/aelf-testnet-sidechain1.zip

Each side chain has its own P2P network, add the testnet sidechain nodes as peer:

    bootnode → ["xxx.xxxx.xxx.xxx:6800", "..."]

``` json
{
    "Network": {
        "BootNodes": [
            "Add the right boot node according sidechain"
        ],
        "ListeningPort": 6800
    }
}
```
