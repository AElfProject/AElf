## Blocks

Blocks are produced by the miners or block producers, the following message definitions show the structure of a Block and its constituents:

```json
message Block {
    BlockHeader Header = 1;
    BlockBody Body = 2;
}

message BlockHeader {
    int32 Version = 1;
    int32 ChainId = 2;
    Hash PreviousBlockHash = 3;
    Hash MerkleTreeRootOfTransactions = 4;
    Hash MerkleTreeRootOfWorldState = 5;
    bytes Bloom = 6;
    int64 Height = 7;
    repeated bytes BlockExtraDatas = 8;
    google.protobuf.Timestamp Time = 9;
    bytes SignerPubkey = 9999;
    bytes Signature = 10000;
}

message BlockBody {
    Hash BlockHeader = 1;
    repeated Hash Transactions = 2;
    repeated Transaction TransactionList = 3;
}

```

A block is the agregation of a BlockHeader and a BlockBody. The header contains metadata about the block itself. It also contains the the merkle root of both the transactions and the world state. The block body is used to contain the transactions that where included in this block by the miner.

#### Block hash

A blockchain is also a data structure of cryptographicaly linked blocks. Inside the header there's the hash of the previous block. The hash of a block uniquely represents a block and become its identifier (sometimes called block id). This hash is based on multiple values, including the **ChainId**, **Height**, **Previous block hash** and **merkle roots** (but not only).

#### Signature

The Sig field is destined to host the signature of the producer, to confirm that he created this block. It is the hash of the header that is signed, not the entire block.

#### Merkle tree

The header contains the merkle tree of the transactions that where included in the block. It also contains the merkle tree of the state which is the merkle tree formed by the hash of the txid and the status of its transaction result.
