# Cross chain transfer

This article will explain how to transfer tokens across chains. It assumes a side chain is already deployed and been indexed by the main-chain.

The transfer will always use the same contract methods and the following two steps:
- initiate the transfer
- receive the tokens

## Prepare
Few preparing steps are required before cross chain transfer, which is to be done only once for one chain. Just ignore this preparing part if already completed.
Let's say that you want to transfer token `FOO` from chain `A` to chain `B`. Note that make sure you are clear about how cross chain transaction verification works before you start.

- Validate **Token Contract** address on chain `A`. 

    Send transaction `tx_1` to **Genesis Contract** with method ValidateSystemContractAddress. You should provide **system_contract_hash_name** 
    and address of **Token Contract** . `tx_1` would be packed in block successfully.
    
  ```protobuf
    rpc ValidateSystemContractAddress(ValidateSystemContractAddressInput) returns (google.protobuf.Empty){}
  
    message ValidateSystemContractAddressInput {
        aelf.Hash system_contract_hash_name = 1;
        aelf.Address address = 2;
    }
    ```

- Register token contract address of chain `A` on chain `B`. 
    
    You need create a proposal on chain `B` which is proposed to **RegisterCrossChainTokenContractAddress**. Apart from cross chain verification context needed, 
    you should also provide the origin data of `tx_1` and **Token Contract** address on chain `A`.
    
  ```protobuf
    rpc RegisterCrossChainTokenContractAddress (RegisterCrossChainTokenContractAddressInput) returns (google.protobuf.Empty) {}
  
    message RegisterCrossChainTokenContractAddressInput{
        int32 from_chain_id = 1;
        int64 parent_chain_height = 2;
        bytes transaction_bytes = 3;
        aelf.MerklePath merkle_path = 4;
        aelf.Address token_contract_address = 5;
    }
    ```

- Validate **TokenInfo** of `FOO` on chain `A`. 

    Send transaction `tx_2` to **Token Contract** with method ValidateTokenInfoExists. You should provide **TokenInfo** of `FOO`. `tx_2` would be packed in block successfully.
    
  ```protobuf
    rpc ValidateTokenInfoExists(ValidateTokenInfoExistsInput) returns (google.protobuf.Empty){}
  
    message ValidateTokenInfoExistsInput{
        string symbol = 1;
        string token_name = 2;
        int64 total_supply = 3;
        int32 decimals = 4;
        aelf.Address issuer = 5;
        bool is_burnable = 6;
        int32 issue_chain_id = 7;
        bool is_profitable = 8;
    }
    ```
- Create token `FOO` on chain `B`. 
    
    Send transaction `tx_2` to **Token Contract** with method CrossChainCreateToken on chain `B`. You should provide the origin data of `tx_2` and cross chain verification context of `tx_2`.
    
  ```protobuf
    rpc CrossChainCreateToken(CrossChainCreateTokenInput) returns (google.protobuf.Empty) {}
  
    message CrossChainCreateTokenInput {
        int32 from_chain_id = 1;
        int64 parent_chain_height = 2;
        bytes transaction_bytes = 3;
        aelf.MerklePath merkle_path = 4;
    }
    ```

You can launch cross chain transfer now.

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

rpc GetBoundParentChainHeightAndMerklePathByHeight (aelf.SInt64Value) returns (CrossChainMerkleProofContext) {
    option (aelf.is_view) = true;
}

message CrossChainMerkleProofContext {
    int64 bound_parent_chain_height = 1;
    aelf.MerklePath merkle_path_from_parent_chain = 2;
}
```

Let's review the fields of the input:
- **from_chain_id**: the source chain id (the chain that issued the tokens).
- **parent_chain_height**: 
  - main-chain to side-chain: the height of the block on the source chain that includes the **CrossChainTransfer** transaction (or more precisely, the block that indexed the transaction).
  - side-chain to side-chain or side-chain to main-chain: this height is the result of **GetBoundParentChainHeightAndMerklePathByHeight** (input is the height of the *CrossChainTransfer*) - accessible in the **bound_parent_chain_height** field.
- **transfer_transaction_bytes**: the serialized form of the **CrossChainTransfer** transaction.
- **merkle_path**: the cross-chain merkle path. For this, two cases to consider:
  - main-chain to side-chain transfer: for this you just need the merkle path from the main-chain's web api with the **GetMerklePathByTransactionIdAsync** method (**CrossChainTransfer** transaction ID as input).
  - side-chain to side-chain or side-chain to main-chain: for this you also need to get the merkle path from the source node (side-chain here). But you also have to complete this merkle path with **GetBoundParentChainHeightAndMerklePathByHeight** with the cross-chain *CrossChainTransfer* transaction's block height (concat the merkle path nodes). The nodes are in the **merkle_path_from_parent_chain** field of the **CrossChainMerkleProofContext** object.