# Running multi-nodes with Docker

This section will walk you through the steps for launching three nodes or more by repeating the steps. It is largely based on the section about running a single node (quickstart), so following the fist tutorial is highly recommended. A pre-requisite for this tutorial is to install Docker on your system.

Here's the main points of the tutorial:
- set up some folders that will contain the configuration files for each node.
- use **aelf-command create** to create a key-pair for all nodes.
- modify the **NodeAccount**, **NodeAccountPassword** values in the **appsettings.json**.
- add the key-pair public key as a miner in the **InitialMinerList**.
- launch the nodes with Docker.

Make sure your Redis instance is ready, this tutorial requires three clean instances (we'll use db1, db2 and db3).

## Pull AElf Docker image

You can start by opening three terminals, in one of them make sure you have the latest version of the image:

```bash
docker pull aelf/node
```

Wait for any update to finish.

### Create configuration

First, choose a location for the folders, for this tutorial we'll create a directory called **MultiNodeTutorial**, that will become your workspace and navigate inside it.

```bash
mkdir MultiNodeTutorial
cd MultiNodeTutorial
```

Create three folders in the workspace folder, one for each miner (lets say **miner1**, **miner2** and **miner3**). 

```bash
mkdir miner1 miner2 miner3
```

Next in each of these folders place an **appsettings.json** file. An example can be found [**here**](https://github.com/AElfProject/AElf/blob/dev/src/AElf.Launcher/appsettings.json).

We'll modify these later in the next steps.

### Accounts

Each node will have it's own account, because they will be independent miners.

Generate three accounts, one for each miner, be sure to keep the addresses and the password as well as the password.

```bash
aelf-command create
```

Be sure to note the public-key and address of all accounts.

### Configuration

In this section we will modify the configuration for each miner.

#### Miners list

Modify each miner's configuration with their respective accounts (**NodeAccount** and **NodeAccountPassword**). 

Once this is done you should update config files with public keys, so the configuration for **InitialMinerList** will look something like this in miner1, miner2 and miner3's configuration files:

- MultiNodeTutorial/miner{1, 2 and 3}/appsettings.json:
```json
"InitialMinerList" : [
    "0499d3bb14337961c4d338b9729f46b20de8a49ed38e260a5c19a18da569462b44b820e206df8e848185dac6c139f05392c268effe915c147cde422e69514cc927",
    "048397dfd9e1035fdd7260329d9492d88824f42917c156aef93fd7c2e3ab73b636f482b8ceb5cb435c556bfa067445a86e6f5c3b44ae6853c7f3dd7052609ed40b",
    "041cc962a51e7bbdd829a8855eca8a03fda708fdf31969251321cb31edadd564bf3c6e7ab31b4c1f49f0f206be81dbe68a75c70b293bf9d04d867ee5e415d3bf8a"
],
```

Note you need to replace these three public keys with the ones you previously created.

#### Network

The next section we need to configure is the network options. Following is miner1's configuration of the **Network** section:

```json
"Network": {
    "BootNodes": [ ** insert other nodes P2P address here ** ],
    "ListeningPort": ** the port your node will be listening on **,
},
```

Only two options need to be changed for this tutorial, **BootNodes** and **ListeningPort**. The listening port the node will be using to be reachable on the network: other nodes will use this to connect to the node. The boot nodes field is a list of peer addresses that the node will connect to on when it's started. So in order for three nodes to connect to others replace the configurations like following:

- MultiNodeTutorial/miner1/appsettings.json:
```json
  "Network": {
    "BootNodes": ["192.168.1.70:6802","192.168.1.70:6803"],
    "ListeningPort": 6801
  },
```

- MultiNodeTutorial/miner2/appsettings.json:
```json
  "Network": {
    "BootNodes": ["192.168.1.70:6801","192.168.1.70:6803"],
    "ListeningPort": 6802
  },
```

- MultiNodeTutorial/miner3/appsettings.json:
```json
  "Network": {
    "BootNodes": ["192.168.1.70:6801","192.168.1.70:6802"],
    "ListeningPort": 6803
  },
```

**Replace** "192.168.1.70" with the correct addresses of each node.

#### Redis

Each node will need it's own database, so in miner2 and miner3 you'll need to change the database number:

- MultiNodeTutorial/miner1/appsettings.json:
```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=1",
    "StateDb": "redis://192.168.1.70:6379?db=1"
  },
```

- MultiNodeTutorial/miner2/appsettings.json:
```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=2",
    "StateDb": "redis://192.168.1.70:6379?db=2"
  },
```

- MultiNodeTutorial/miner3/appsettings.json:
```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=3",
    "StateDb": "redis://192.168.1.70:6379?db=3"
  },
```

**Replace** "192.168.1.70" and 6379 with whatever host your Redis server is on.

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

The example shows that the port is 8000, for miner1 you can keep this value but since we're running this tutorial on a single machine, miner2 and miner3's port must be different, lets say 8001 and 8002.

### Start docker - node one 

```bash
docker run -it -p 8000:8000 -p 6800:6800 -v <your/local/keystore/path>:/root/.local/share/aelf/keys -v <path/to/MultiNodeTutorial/miner1/appsettings.json>:/app/appsettings.json aelf/node:latest /bin/bash
```

Here 8000 will be the API endpoint port and 6800 the listening port and both are mapped in docker. You will need to replace "your/local/keystore/path" to the location of you key store (aelf-command create will show you where your keys are currently stored).

You also need to map the configuration files we created, in the miner1, miner2 and miner3 directories, so replace "path/to/MultiNodeTutorial/miner1/appsettings.json" with your local path.

Next you can launch the node:

```bash
dotnet AElf.Launcher.dll
```

The next step is the same, but with a second container.

### Start docker - node one 

```bash
docker run -it -p 8001:8001 -p 6801:6801 -v <your/local/keystore/path>:/root/.local/share/aelf/keys -v <path/to/MultiNodeTutorial/miner2/appsettings.json>:/app/appsettings.json aelf/node:latest /bin/bash
```

Here 8001 will be the API endpoint port and 6801 the listening port and both are mapped in docker. Like for the previous node,you will need to replace "your/local/keystore/path" to the location of you key store.

You also need to map the configuration files we created for miner2, in the miner2 directory, so replace "path/to/MultiNodeTutorial/miner2/appsettings.json" with your local path.

```bash
dotnet AElf.Launcher.dll
```

The next step is the same, but with a third container.

### Start docker - node one 

```bash
docker run -it -p 8002:8002 -p 6802:6802 -v <your/local/keystore/path>:/root/.local/share/aelf/keys -v <path/to/MultiNodeTutorial/miner3/appsettings.json>:/app/appsettings.json aelf/node:latest /bin/bash
```

Here 8002 will be the API endpoint port and 6802 the listening port and both are mapped in docker. Like for the previous node,you will need to replace "your/local/keystore/path" to the location of you key store.

You also need to map the configuration files we created for miner3, in the miner3 directory, so replace "path/to/MultiNodeTutorial/miner3/appsettings.json" with your local path.

```bash
dotnet AElf.Launcher.dll
```

## Access the node's Swagger

You can now check all nodes with the Swagger interface. Open the following addresses in your browser:

```bash
http://your-ip:8000/swagger/index.html
http://your-ip:8001/swagger/index.html
http://your-ip:8002/swagger/index.html
```

The ip should be localhost if your browser and docker are running local.

From here you can try out any of the available API commands on the Swagger page. You can also have a look at the API reference [**here**](../../web-api-reference/reference.md).