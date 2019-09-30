## Blocks

Blocks are produced by the miners or block producers, the following message definitions show the structure of a Block and its constituents:

```json
message Block {
    BlockHeader header = 1;
    BlockBody body = 2;
}

message BlockHeader {
    int32 version = 1;
    int32 chain_id = 2;
    Hash previous_block_hash = 3;
    Hash merkle_tree_root_of_transactions = 4;
    Hash merkle_tree_root_of_world_state = 5;
    bytes bloom = 6;
    int64 height = 7;
    repeated bytes extra_data = 8;
    google.protobuf.Timestamp time = 9;
    Hash merkle_tree_root_of_transaction_status = 10;
    bytes signer_pubkey = 9999;
    bytes signature = 10000;
}

message BlockBody {
    repeated Hash transaction_ids = 1;
}

```

A block is the aggregation of a BlockHeader and a BlockBody. The header contains metadata about the block itself. It also contains the the merkle roots of both the transactions and the world state. The block body is used to contain the transaction ids that where included in this block by the miner.

#### Block hash

A blockchain is also a data structure of cryptographically linked blocks. Inside the header there's the hash of the previous block. The hash of a block uniquely represents a block and become its identifier (sometimes called block id). This hash is based on multiple values, including the **chain id**, the **height** of the block in the chain, the previous **blocks hash** and **merkle roots** (but not only).

#### Signature

The **signature** field is destined to host the signature of the producer of that block, to confirm that he created this block. It is the hash of the header that is signed, not the entire block.

#### Merkle tree

The header contains the merkle tree of the transactions that where included in the block. It also contains the merkle tree of the state which is the merkle tree formed by the hash of the txid and the status of its transaction result.
