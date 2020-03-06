# Running a side chain (after its release)

This tutorial will explain how to run a side-chain node after it has been *approved* by the producers and *released* by the creator. After the creation of the side-chain, the producers need to run a side-chain node.

A side-chain node is usually very similar to a main-chain node because both are based on AElf software and have common modules. The main difference is the configuration which varies depending on if the node is a side chain or not.

Note: this tutorial assumes the following:
- you already have a main-chain node running.
- you are a producer on the main-chain.
- the creation of the side-chain has already been approved and released.

It's also **important** to know that the key-pair (account) used for mining on the side-chain must be the **same** as the one you use for mining on the main-chain. Said in another way both production nodes need to be launched with the **same** key-pair.

Note: for more information about the side-chain creation, refer to the document in the [cross-chain section](../../crosschain/setup.md).

### Side chain configuration:

Two configuration files must be placed in the configuration folder of the side-chain, this is also the folder from which you will launch the node:
- appsettings.json
- appsettings.SideChain.MainNet.json

After the *release* of the side-chain creation request, the **ChainId** of the new side-chain will be accessible in the **SideChainCreatedEvent** logged by the transaction that released.

In this example, we will set up the side-chain node with **tDVV** (1866392 converted to base58) as it's chain id, connecting to Redis' **db2**. The web API port is **1235**. To make the tutorial easier to follow the node's account will be the same as the miner (used below in the miner list). So don't forget to change the **account**, **password** and **initial miner**. You can use the same account for both nodes in this tutorial.

If at the time of launching the side-chain the P2P addresses of the other producers is know, they should be added to the bootnodes in the configuration of the side-chain.

In **appsettings.json** change the following configuration sections:
```json
"ChainId":"tDVV",
"ChainType":"SideChain",
"NetType": "MainNet",
"ConnectionStrings": {
        "BlockchainDb": "redis://localhost:6379?db=2",
        "StateDb": "redis://localhost:6379?db=2"
},
"Account": {
    "NodeAccount": "YOUR PRODUCER ACCOUNT",
    "NodeAccountPassword": "YOUR PRODUCER PASSWORD"
},
"Kestrel": {
    "EndPoints": {
        "Http": {
            "Url": "http://*:1235/"
        }
    }
},
"Consensus": {
    "InitialMinerList": ["THE PUB KEY OF THE ACCOUNT CONFIGURED EARLIER"],
    "MiningInterval": 4000,
    "StartTimestamp": 0
},

In **appsettings.SideChain.MainNet.json** change the following configuration sections:

```json
{
  "CrossChain": {
    "Grpc": {
      "ParentChainServerPort": 5010,
      "ListeningPort": 5000,
      "ParentChainServerIp": "127.0.0.1"
    },
    "ParentChainId": "AELF",
    "MaximalCountForIndexingParentChainBlock" : 32
  }
}
```

Change **ParentChainServerIp** and **ParentChainServerPort** depending on the listening address of your mainchain node.

### Launch the side-chain node:

Open a terminal and navigate to the folder where you created the configuration for the side-chain.

```bash
dotnet ../AElf.Launcher.dll
```

You can try out a few commands from another terminal to check if everything is fine, for example:

```bash
aelf-command get-blk-height -e http://127.0.0.1:1235
```