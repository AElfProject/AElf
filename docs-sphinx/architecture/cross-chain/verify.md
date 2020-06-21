# Cross chain verify

This section will explain how to verify a transaction across chains. It assumes a side chain is already deployed and been indexed by the main-chain.

## Send a transaction

Any transaction with status `Mined` can be verified, the only pre-condition is that the transaction was indexed.

## Verify the transaction

There's basically two scenarios that can be considered:
- verifying a main-chain transaction on a side chain.
- verifying a side-chain transaction on the main-chain or another side-chain.

```protobuf
rpc VerifyTransaction (VerifyTransactionInput) returns (google.protobuf.BoolValue) {
  option (aelf.is_view) = true;
}

message VerifyTransactionInput {
  aelf.Hash transaction_id = 1;
  aelf.MerklePath path = 2;
  sint64 parent_chain_height = 3;
  int32 verified_chain_id = 4;
}

```

**VerifyTransaction** is the view method of the cross-chain contract and that will be used to perform the verification. It returns whether the transaction was mined and indexed by the destination chain. This method will be used in both scenarios, what differs is the input:

### Verify transaction from main-chain on the side-chain

Verifying a transaction sent on the main-chain on a side chain, you can call **VerifyTransaction** on the side-chain with the following input values:
  - parent_chain_height - the height of the block, on the main-chain, in which the transaction was packed.
  - transaction_id - the ID of the transaction that you want to verify.
  - path - the merkle path from the main-chain's web api with the **GetMerklePathByTransactionIdAsync** with the ID of the transaction.
  - verified_chain_id - the source chainId, here the main chain's.

You can get the `MerklePath`  of  transaction in one block which packed it by chain's web api with the **GetMerklePathByTransactionIdAsync** (See [web api reference](../../reference/web-api/web-api)).

### Verify transaction from side-chain on the main-chain or another side-chain

Before you can verify a transaction sent on a side-chain, on the main-chain or on another side-chain, you need use another contract api **GetBoundParentChainHeightAndMerklePathByHeight** from `cross chain contract`, on the source chain, in which the transaction was packed.

The input of this api is the height of block which packed the transaction. And it will return merkle proof context

```Protobuf
rpc GetBoundParentChainHeightAndMerklePathByHeight (google.protobuf.Int64Value) returns (CrossChainMerkleProofContext) {
    option (aelf.is_view) = true;
}

message CrossChainMerkleProofContext {
    int64 bound_parent_chain_height = 1;
    aelf.MerklePath merkle_path_from_parent_chain = 2;
}
```

 With the result returned by above api, you can call **VerifyTransaction** on the main-chain (or the other side-chain) with the following input values:
- transaction_id - the ID of the transaction that you want to verify.
- parent_chain_height - use the **bound_parent_chain_height** field of **CrossChainMerkleProofContext** .
- path - the concatenation of 2 merkle paths, in order:
  - the merkle path of the transaction, use the web api method **GetMerklePathByTransactionIdAsync**. 
  - next, use **GetBoundParentChainHeightAndMerklePathByHeight** and use the **merkle_path_from_parent_chain** field from the **CrossChainMerkleProofContext** object.
- verified_chain_id - the source chainId, here the side chain on which the transaction was mined.


