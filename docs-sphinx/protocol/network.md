# Network

## 1. Introduction

The role that the network layer plays in AElf is very important, it maintains active and healthy connections to other peers of the network and is of course the medium through which nodes communicate and follow the chain protocol. The network layer also implements interfaces for higher-level logic like the synchronization code and also exposes some functionality for the node operator to administer and monitor network operations.

The design goals when designing AElf’s network layer was to avoid “reinventing the wheel” and keep things as simply possible, we ended up choosing gRPC to implement the connections in AElf. Also, it was important to isolate the actual implementation (the framework used) from the contract (the interfaces exposed to the higher-level layers) to make it possible to switch implementation in the future without breaking anything.

## 2. Architecture

This section will present a summary of the different layers that are involved in network interactions.

The network is split into 3 different layers/projects, namely: 
- AElf.OS 
    - Defines event handles related to the network.
    - Defines background workers related to the network.
- AElf.OS.Core.Network 
    - Defines service layer exposed to higher levels.
    - Contains the definitions of the infrastructure layer.
    - Defines the component, types.
- AElf.OS.Network.Grpc 
    - The implementation of the infrastructure layer.
    - Launches events defined in the core
    - Low-level functionality: serialization, buffering, retrying...

### 2.1 AElf.OS

At the AElf.OS layer, the network monitors events of interest to the network through event handlers, such as kernel layer transaction verification, block packaging, block execution success, and discovery of new libs. The handler will call NetworkService to broadcast this information to its connected peer. And it will run background workers to process network tasks regularly.

Currently, the AElf.OS layer handles those events related to the network:
- Transaction Accepted Event：the event that the transaction pool receives the transaction and passes verification。
- Block Mined Event：when the current node is BP, the event that the block packaging is completed.
- Block Accepted Event：the event that the node successfully executes the block.
- New Irreversible Block Found Event：the event that the chain found the new irreversible block.

Currently, the AElf.OS layer will periodically process the following tasks.
- Peer health check: regularly check whether the connected peer is healthy and remove the abnormally connected peer.
- Peer retry connection: peer with abnormal connection will try to reconnect.
- Network node discovery: regularly discover more available nodes through the network.

### 2.2 AElf.OS.Core.Network
AElf.OS.Core.Network is the core module of the network，contains services(service layer exposed to higher levels (OS)) and definitions (abstraction of the Infrastructure layer).
- Application layer implementation: 
    - NetworkService: this service exposes and implements functionality that is used by higher layers like the sync and RPC modules. It takes care of the following:
        - sending/receiving: it implements the functionality to request a block(s) or broadcast items to peers by using an IPeerPool to select peers. This pool contains references to all the peers that are currently connected.
        - handling network exceptions: the lower-level library that implements the Network layer is expected to throw a NetworkException when something went wrong during a request.
- Infrastructure layer implementation and definition:
    - IPeerPool/PeerPool: manages active connections to peers.
    - IPeer: an active connection to a peer. The interface defines the obvious request/response methods, it exposes a method for the NetworkService to try and wait for recovery after some network failure. It contains a method for getting metrics associated with the peer. You can also access information about the peer itself (ready for requesting, IP, etc.).
    - IAElfNetworkServer: manages the lifecycle of the network layer, implements listening for connections, it is the component that accepts connections. For now, it is expected that this component launches NetworkInitializationFinishedEvent when the connection to the boot nodes is finished.
- Definitions of types (network_types.proto and partial).
- Defines the event that should be launched from the infrastructure layer’s implementation.

### 2.3 AElf.OS.Network.Grpc

The AElf.OS.Network.Grpc layer is the network infrastructure layer that we implement using the gRPC framework.
- GrpcPeer：implemented the interface IPeer defined by the AElf.OS.Core.Network layer
- GrpcNetworkServer: implemented the interface IAElfNetworkServer defined by the AElf.OS.Core.Network layer
- GrpcServerService: implemented network service interfaces, including interfaces between nodes and data exchange.
- Extra functionality:
    - Serializing requests/deserializing responses (protobuf).
    - Some form of request/response mechanism for peers (optionally with the timeout, retry, etc).
    - Authentification.

In fact, gRPC is not the only option. Someone could if they wanted to replace the gRPC stack with a low-level socket API (like the one provided by the dotnet framework) and re-implement the needed functionality. As long as the contract (the interface) is respected, any suitable framework can be used if needed.

## 3. Protocol

Each node implements the network interface protocol defined by AElf to ensure normal operation and data synchronization between nodes.

### 3.1 Connection

#### 3.1.1 DoHandshake Interface

When a node wants to connect with the current node, the current node receives the handshake information of the target node through the interface DoHandshake. After the current node verifies the handshake information, it returns the verification result and the handshake information of the current node to the target node.

The handshake information, in addition to being used in the verification of the connection process, will also record the status of the other party's chain after the connection is successful, such as the current height, Lib height, etc.

``` Protobuf
rpc DoHandshake (HandshakeRequest) returns (HandshakeReply) {}
```

- Handshake Message

    ``` Protobuf
    message Handshake {
        HandshakeData handshake_data = 1;
        bytes signature = 2;
        bytes session_id = 3;
    }
    ```

    - handshake_data: the data of handshake.
    - signature: the signatrue of handshake data.
    - session_id: randomly generated ids when nodes connect.
    <br/>

- HandshakeData Message
    ``` Protobuf
    message HandshakeData {
        int32 chain_id = 1;
        int32 version = 2;
        int32 listening_port = 3;
        bytes pubkey = 4;
        aelf.Hash best_chain_hash = 5;
        int64 best_chain_height = 6;
        aelf.Hash last_irreversible_block_hash = 7;
        int64 last_irreversible_block_height = 8;
        google.protobuf.Timestamp time = 9;
    }
    ```
    - chain_id: the id of current chain.
    - version: current version of the network.
    - listening_port: the port number at which the current node network is listening.
    - pubkey: the public key of the current node used by the receiver to verify the data signature.
    - best_chain_hash: the lastest block hash of the best branch.
    - best_chain_height: the lastest block height of the best branch.
    - last_irreversible_block_hash: the hash of the last irreversible block.
    - last_irreversible_block_height: the height of the last irreversible block.
    - time: the time of handshake.
    <br/>

- HandshakeRequest Message
    ``` Protobuf
    message HandshakeRequest {
        Handshake handshake = 1;
    }
    ```

    - handshake: complete handshake information, including handshake data and signature.
    <br/>

- HandshakeReply Message
    ``` Protobuf
    message HandshakeReply {
        Handshake handshake = 1;
        HandshakeError error = 2;
    }
    ```
    - handshake: complete handshake information, including handshake data and signature.
    - error: handshake error enum.
    <br/>

- HandshakeError Enum
    ``` Protobuf
    enum HandshakeError {
        HANDSHAKE_OK = 0;
        CHAIN_MISMATCH = 1;
        PROTOCOL_MISMATCH = 2;
        WRONG_SIGNATURE = 3;
        REPEATED_CONNECTION = 4;
        CONNECTION_REFUSED = 5;
        INVALID_CONNECTION = 6;
        SIGNATURE_TIMEOUT = 7;
    }
    ```
    - HANDSHAKE_OK: indicate no error actually; the default value. 
    - CHAIN_MISMATCH: the chain ID does not match.
    - PROTOCOL_MISMATCH: the network version does not match.
    - WRONG_SIGNATURE: the signature cannot be verified.
    - REPEATED_CONNECTION: multiple connection requests were sent by the same peer.
    - CONNECTION_REFUSED: peer actively rejects the connection, either because the other party's connection pool is slow or because you have been added to the other party's blacklist.
    - INVALID_CONNECTION: connection error, possibly due to network instability, causing the request to fail during the connection.
    - SIGNATURE_TIMEOUT: the signature data has timed out.
    <br/>

#### 3.1.2 ConfirmHandshake Interface

When the target node verifies that it has passed the current node's handshake message, it sends the handshake confirmation message again.

``` Protobuf
rpc ConfirmHandshake (ConfirmHandshakeRequest) returns (VoidReply) {}
```

``` Protobuf
message ConfirmHandshakeRequest {
}
```

### 3.2 Broadcasting

#### 3.2.1 BlockBroadcastStream Interface

The interface BlockCastStream is used to receive information about the block and its complete transaction after the BP node has packaged the block.

``` Protobuf
rpc BlockBroadcastStream (stream BlockWithTransactions) returns (VoidReply) {}
```

``` Protobuf
message BlockWithTransactions {
   aelf.BlockHeader header = 1;
   repeated aelf.Transaction transactions = 2;
}
```

- header:
- transactions:

#### 3.2.2 TransactionBroadcastStream Interface

Interface TransactionBroadcastStream used to receive other nodes forward transaction information.

``` Protobuf
rpc TransactionBroadcastStream (stream aelf.Transaction) returns (VoidReply) {}
```

#### 3.2.3 AnnouncementBroadcastStream Interface

Interface AnnouncementBroadcastStream used to receive other nodes perform block after block information broadcast.

``` Protobuf
rpc AnnouncementBroadcastStream (stream BlockAnnouncement) returns (VoidReply) {}
```

``` Protobuf
message BlockAnnouncement {
   aelf.Hash block_hash = 1;
   int64 block_height = 2;
}
```

- block_hash: the announced block hash.
- block_height: the announced block height.

#### 3.2.4 LibAnnouncementBroadcastStream Interface

Interface LibAnnouncementBroadcastStream used to receive other nodes Lib changed Lib latest information broadcast.

``` Protobuf
rpc LibAnnouncementBroadcastStream (stream LibAnnouncement) returns (VoidReply) {}
```

``` Protobuf
message LibAnnouncement{
   aelf.Hash lib_hash = 1;
   int64 lib_height = 2;
}
```

- lib_hash: the announced last irreversible block hash.
- lib_height: the announced last irreversible block height.

### 3.3 Block Request

#### 3.3.1 RequestBlock Interface

The interface RequestBlock requests a single block in response to other nodes. Normally, the node receives block information packaged and broadcast by BP. However, if the block is not received for some other reason. The node may also receive BlockAnnouncement messages that are broadcast after the block has been executed by other nodes, so that the complete block information can be obtained by calling the RequestBlock interface of other peers.

``` Protobuf
rpc RequestBlock (BlockRequest) returns (BlockReply) {}
```

- BlockRequest Message
    ``` Protobuf
    message BlockRequest {
        aelf.Hash hash = 1;
    }
    ```

    - hash: the block hash that you want to request.
    <br/>

- BlockReply Message
    ``` Protobuf
    message BlockReply {
        string error = 1;
        BlockWithTransactions block = 2;
    }
    ```
    - error: error message.
    - block: the requested block, including complete block and transactions information.
    <br/>

#### 3.3.2 RequestBlocks Interface

The interface RequestBlock requests blocks in bulk in response to other nodes. When a node forks or falls behind, the node synchronizes blocks by bulk fetching a specified number of blocks to the RequestBlocks interface through which the target node is called.

``` Protobuf
rpc RequestBlocks (BlocksRequest) returns (BlockList) {}
```

- BlocksRequest Message
    ``` Protobuf
    message BlocksRequest {
        aelf.Hash previous_block_hash = 1;
        int32 count = 2;
    }
    ```

    - previous_block_hash: the previous block hash of the request blocks, and the result does not contain this block.
    - count: the number of blocks you want to request.
    <br/>

- BlockList Message
    ``` Protobuf
    message BlockList {
        repeated BlockWithTransactions blocks = 1;
    }
    ```
    - blocks: the requested blocks, including complete blocks and transactions information.
    <br/>

### 3.4 Peer Management

#### 3.4.1 Ping Interface

Interface Ping is used between nodes to verify that each other's network is available.

``` Protobuf
rpc Ping (PingRequest) returns (PongReply) {}
```

``` Protobuf
message PingRequest {
}
```

``` Protobuf
message PongReply {
}
```

#### 3.4.2 CheckHealth Interface

The interface CheckHealth is invoked for other nodes' health checks, and each node periodically traverses the available peers in its own Peer Pool to send health check requests and retries or disconnects if an exception in the Peer state is found.

``` Protobuf
rpc CheckHealth (HealthCheckRequest) returns (HealthCheckReply) {}
```

``` Protobuf
message HealthCheckRequest {
}
```

``` Protobuf
message HealthCheckReply {
}
```