# Cross chain transfer

This article will explain how to transfer tokens across chains. It assumes a side chain is already deployed and been indexed by the main-chain.

The transfer will always use the same contract methods and the following two steps:
- initiate the transfer
- receive the tokens

## Initiate the transfer

On the token contract, it's the **CrossChainTransfer** method that is used to trigger the transfer:

```protobuf
rpc CrossChainTransfer (CrossChainTransferInput) returns (google.protobuf.Empty) { }

message CrossChainTransferInput {
    aelf.Address to = 1; 
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
    int32 to_chain_id = 5; 
    int32 issue_chain_id = 6;
}
```

Let's review the fields of the input:
- **to**: this is the address on the destination chain that will receive the tokens.
- **symbol** and **amount**: the token and amount to be transferred.
- **issue_chain_id** and **to_chain_id**: respectively the source (the chain on which the token was issued) and destination chain id (destination is the chain on which the tokens will be received).

## Receive on the destination chain

On the destination chain tokens need to be received, it's the **CrossChainReceiveToken** method that is used to trigger the reception:

```protobuf
rpc CrossChainReceiveToken (CrossChainReceiveTokenInput) returns (google.protobuf.Empty) { }

message CrossChainReceiveTokenInput {
    int32 from_chain_id = 1;
    int64 parent_chain_height = 2;
    bytes transfer_transaction_bytes = 3;
    aelf.MerklePath merkle_path = 4;
}
```

Let's review the fields of the input:
- **from_chain_id**: the source chain id (the chain that issued the tokens).
- **parent_chain_height**: the height of the block on the source chain that includes the **CrossChainTransfer** transaction (or more precisely, the block that indexed the transaction).
- **transfer_transaction_bytes**: the serialized form of the **CrossChainTransfer** transaction.
- **merkle_path**: the cross-chain merkle path. This you can get from the origin chain's web api with the **GetMerklePathByTransactionIdAsync** method. This takes the **CrossChainTransfer** transaction ID as input.