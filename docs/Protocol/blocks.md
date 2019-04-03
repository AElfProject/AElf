## Blocks

Blocks are objects that are produced by the miners or block producers, the following message definitions show the structure of a Block and its constituents:

```json
message Block {
    BlockHeader Header = 1;
    BlockBody Body = 2;
}

message BlockHeader {
    int32 Version = 1;
    Hash PreviousBlockHash = 2;
    Hash MerkleTreeRootOfTransactions = 3;
    Hash MerkleTreeRootOfWorldState = 4;
    bytes Bloom = 5;
    int64 Height = 6;
    bytes Sig = 7;
    bytes P = 8;
    google.protobuf.Timestamp Time = 9;
    int32 ChainId = 10;
    repeated bytes BlockExtraDatas = 11;
}

message BlockBody {
    Hash BlockHeader = 1;
    repeated Hash Transactions = 2;
    repeated Transaction TransactionList = 3;
}

```

A block is the agregation of a BlockHeader and a BlockBody. The header contains metadata about the block itself, such as the previous block hash. It also contains the the merkle root of both the transactions and the world state.
The block can also be signed and this signature will be place in the *Sig* field.
The block body is used to contain the transactions that where included in this block by the miner.