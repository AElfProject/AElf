## Setup cross chain

### Node type configuration:

When a node wants to be a side chain to the mainchain, it needs to be configured accordingly. Based on the value of the **ChainType** configuration value, the launcher executable will register and launch either a mainchain node or a side chain node. Note that both have many similarities because they’re both based on a common module. Both node types will have a blockchain node context, this provides access to the p2p server and node’s context. When starting an initial list of contracts is loaded. Both chain of course need to be set up with their own chain id, networking configuration...

### Procedure

This guides you through cross-chain configuration.

1. Create configs:

Modify the **ChainId** and **CrossChain** sections of the configuration files.

- LocalServerIP/Port is the listening endpoint of the side chain communication link.

### Main chain:

Keep ```ParentChainId``` empty as main chain doesn't have parent chain. 

```json
"ChainId":"AELF",
"ChainType":"MainChain",
"CrossChain": {
    "Grpc": {
      "LocalServerPort": 5000,
      "LocalServerHost": "127.0.0.1"
    },
    "ParentChainId":"",
}
```

### Side chain:

```json
"ChainId":"AELF2",
"ChainType":"SideChain",
"CrossChain":{
    "Grpc": {
      "RemoteParentChainServerPort": 5000,
      "LocalServerHost": "127.0.0.1",
      "LocalServerPort": 5010,
      "RemoteParentChainServerHost": "127.0.0.1"
    },
    "ParentChainId":"AELF",
}
```

ServerPort/ServerHost are the listening port/IP.








