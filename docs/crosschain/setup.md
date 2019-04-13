## setup cross chain

### Node type configuration:

When a node wants to be a side chain to the mainchain, it needs to be configured accordingly. Based on the value of the **ChainType** configuration value, the launcher executable will register and launch either a mainchain node or a side chain node. Note that both have many similarities because they’re both based on a common module. Both node types will have a blockchain node context, this provides access to the p2p server and node’s context. When starting an initial list of contracts is loaded. Both chain of course need to be set up with their own chain id, networking configuration...

### Procedure

This guides you through cross-chain configuration.

1. Gen certifs

```json
aelf-cli gen-cert aelf 127.0.0.1
aelf-cli gen-cert aelf2 127.0.0.1
aelf-cli gen-cert aelf3 127.0.0.1
```

Generates *.cert.pem and *.key.pem files for each of the chains, replace the start with the chain id you specify.

2. Create configs:

Modify the **ChainId** and **CrossChain** sections of the configuration files.

- LocalServerIP/Port is the listening endpoint of the side chain communication link.
- LocalServer/client TODO
certificate name
- LocalCertificateFileName: the name of the certif associated with the server.

### Main chain:

```json
"ChainId":"AELF",
"ChainType":"Main",
"CrossChain": {
    "Grpc": {
        "LocalServerPort":5000,
        "LocalServerIP":"127.0.0.1",
        "LocalServer":true,
        "LocalClient":false,
        "LocalCertificateFileName":"AELF"
    }
},
"ParentChainId":"",
"ExtraDataSymbols":["Consensus"]
```

### Side chain:

```json
"ChainId":"AELF2",
"ChainType":"Side",
"CrossChain":{
    "Grpc":{
        "RemoteParentChainNodePort":5000,
        "RemoteParentChainNodeIp":"127.0.0.1",
        "RemoteParentCertificateFileName":"AELF",
        "LocalServerPort":5010,
        "LocalServerIP":"127.0.0.1",
        "LocalServer":true,
        "LocalClient":true,
        "LocalCertificateFileName":"2112"
    },
}
"ParentChainId":"AELF",
"ExtraDataSymbols":["Consensus"]
},
```

RemoteParentChainNodePort/NodeIP/Certif are the listening port/IP and the of the main chain.
Extradata: TODO








