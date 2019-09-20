# QuickStart

## Manual build & run the sources

This section will walk you through the steps for launching two nodes or more by repeating the steps. It is largely based on the section 
about running a single node (quickstart), so following the fist tutorial is highly recommended.

Make sure that you can run a single node. To sum the steps for one node:
- clone the repository.
- use **aelf-command create** to create a keypair that is by default saved to the datadir.
- modify the **NodeAccount**, **NodeAccountPassword** values in the **appsettings.json** (AElf.Launcher project).
- add the key-pair public key as a miner in the **InitialMinerList**.
- Launch the node.

These are basically the step you need to repeat to form a network of nodes.

### Setup the environment

In order to setup the environment for a multi-node network workflow is like this:
- create/choose a folder that will be the target of your build.
- create a folder for each node you want to setup.
- add to every folder the configuration files (templates are located in the aelf/src/AElf.Launcher/ folder of the cloned repository).

This section will walk you through these steps of creating this structure.

First, choose a location for the folders, for this tutorial we'll create a directory called **MultiNodeTutorial**, that will become your workspace and navigate inside it.

```bash
mkdir MultiNodeTutorial
cd MultiNodeTutorial
```

Create two folders in the workspace folder, one for each miner (lets say **miner1** and **miner2**). 

```bash
mkdir miner1 miner2
```

From the AElf repository that you cloned, there's templates for **appsettings.json** and **appsettings.MainChain.MainNet.json** copy these two files and place a copy in each of the miners folders. Next we'll generate the accounts and modify the configuration.

### Account and configuration

Generate two accounts, one for each miner, be sure to keep the addresses and the password as well as the password.

```bash
aelf-command create
```

#### Miners

Modify each miners configuration with their respective accounts like in the previous tutorial. Once this is done you should update both config files with both accounts, so the configuration for **InitialMinerList** will look something like this in **both** miner1 and miner2's configuration files:

```json
"InitialMinerList" : [
    "0499d3bb14337961c4d338b9729f46b20de8a49ed38e260a5c19a18da569462b44b820e206df8e848185dac6c139f05392c268effe915c147cde422e69514cc927",
    "048397dfd9e1035fdd7260329d9492d88824f42917c156aef93fd7c2e3ab73b636f482b8ceb5cb435c556bfa067445a86e6f5c3b44ae6853c7f3dd7052609ed40b"
],
```

Note that there's no need to change the default template for **appsettings.MainChain.MainNet.json**.

#### Network

The next section we need to configure is the network options. Following is miner1's configuration of the **Network** section:

```json
"Network": {
    "BootNodes": [ ** insert other nodes P2P address here ** ],
    "ListeningPort": ** the port your node will be listening on **,
},
```

Only two options will be needed for this tutorial, **BootNodes** and **ListeningPort**. The listening port the node will be using to be reachable on the network: other nodes will use this to connect to your node. The boot nodes is a list of address that the node will connect to on when it's started. So in order for miner1 to connect to miner2 replace the configurations like following:

- miner1 :
```json
  "Network": {
    "BootNodes": ["127.0.0.1:6802"],
    "ListeningPort": 6801
  },
```

- miner2:
```json
  "Network": {
    "BootNodes": ["127.0.0.1:6801"],
    "ListeningPort": 6802
  },
```

Note that with this configuration you will see an error printed in the logs. This is normal, when the first node comes online the second is probably not started.

#### Redis

Each node will need it's own database, so in miner2 you'll need to change the database number (here 2):

```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://localhost:6379?db=2",
    "StateDb": "redis://localhost:6379?db=2"
  },
```

#### RPC endpoint

The last configuration option we need to change is the RPC endpoint at which the node's API is reachable.

```json
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://*:8000/"
      }
    }
  },
  ```

The example shows that the port is 8000, for miner1 you can keep this value but since we're running this tutorial on a single machine, miner2 port must be different, lets say 8001.

### Build and launch

First you will need to build/publish AElf. In the **MultiNodeTutorial** create a directory named **aelf-build**, we will use this folder as the target of the build (the executable and all dependencies will be placed in this).

Now use the following command to build, by modifying the path to the cloned repository (**aelf-repo**) and the path to the tutorials workspace (**MultiNodeTutorial**):

```bash
dotnet build ~/**aelf-repo**/src/AElf.Launcher/AElf.Launcher.csproj --configuration Debug -o ~/**MultiNodeTutorial**/aelf-build/
```

You should see a build message indicating "0 Error(s)", you can safely ignore any warnings.

Next open 2 terminals, in the first navigate to miner1's directory and in the second navigate to miner2's directory. In the first then in the second launch the following command:

```bash
dotnet ../AElf.Launcher.dll
```

todo: get block height

** How to run a node and join the existing system **


