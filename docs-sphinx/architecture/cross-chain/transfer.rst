Cross chain transfer
====================

Cross chain transfer is one of mostly used cases when it comes to cross
chain verification. AElf already supports cross chain transfer
functionality in contract. This section will explain how to transfer
tokens across chains. It assumes a side chain is already deployed and
been indexed by the main chain.

The transfer will always use the same contract methods and the following
two steps: - initiate the transfer - receive the tokens

Prepare
-------

Few preparing steps are required before cross chain transfer, which is
to be done only once for one chain. Just ignore this preparing part if
already completed.

Let’s say that you want to transfer token FOO from chain A to chain B.
Note that please make sure you are already clear about how cross chain
transaction verification works before you start. Any input
contains\ ``MerklePath`` in the following steps means the cross chain
verification processing is needed. See :doc:`cross chain verification <crosschain-verification>`
for more details.

-  Validate **Token Contract** address on chain ``A``.

   Send transaction ``tx_1`` to **Genesis Contract** with method
   ValidateSystemContractAddress. You should provide
   **system_contract_hash_name** and address of **Token Contract** .
   ``tx_1`` would be packed in block successfully.

   .. code:: protobuf

        rpc ValidateSystemContractAddress(ValidateSystemContractAddressInput) returns (google.protobuf.Empty){}

        message ValidateSystemContractAddressInput {
            aelf.Hash system_contract_hash_name = 1;
            aelf.Address address = 2;
        }

-  Register token contract address of chain ``A`` on chain ``B``.

   Create a proposal, which is proposed to
   **RegisterCrossChainTokenContractAddress**, for the default
   parliament organization (check :doc:`Parliament contract <../../reference/smart-contract-api/parliament>`
   for more details) on chain ``B``. Apart from cross chain verification
   context, you should also provide the origin data of ``tx_1`` and
   **Token Contract** address on chain ``A``.

   .. code:: protobuf

        rpc RegisterCrossChainTokenContractAddress (RegisterCrossChainTokenContractAddressInput) returns (google.protobuf.Empty) {}

        message RegisterCrossChainTokenContractAddressInput{
            int32 from_chain_id = 1;
            int64 parent_chain_height = 2;
            bytes transaction_bytes = 3;
            aelf.MerklePath merkle_path = 4;
            aelf.Address token_contract_address = 5;
        }

-  Validate **TokenInfo** of ``FOO`` on chain ``A``.

   Send transaction ``tx_2`` to **Token Contract** with method
   **ValidateTokenInfoExists** on chain ``A``. You should provide
   **TokenInfo** of ``FOO``. ``tx_2`` would be packed in block
   successfully.

   .. code:: protobuf

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

-  Create token ``FOO`` on chain ``B``.

   Send transaction ``tx_2`` to **Token Contract** with method
   CrossChainCreateToken on chain ``B``. You should provide the origin
   data of ``tx_2`` and cross chain verification context of ``tx_2``.

   .. code:: protobuf

        rpc CrossChainCreateToken(CrossChainCreateTokenInput) returns (google.protobuf.Empty) {}

        message CrossChainCreateTokenInput {
            int32 from_chain_id = 1;
            int64 parent_chain_height = 2;
            bytes transaction_bytes = 3;
            aelf.MerklePath merkle_path = 4;
        }

Initiate the transfer
---------------------

On the token contract of source chain, it’s the **CrossChainTransfer**
method that is used to trigger the transfer:

.. code:: protobuf

       rpc CrossChainTransfer (CrossChainTransferInput) returns (google.protobuf.Empty) { }

       message CrossChainTransferInput {
           aelf.Address to = 1; 
           string symbol = 2;
           sint64 amount = 3;
           string memo = 4;
           int32 to_chain_id = 5; 
           int32 issue_chain_id = 6;
       }

The fields of the input: - to : the target address to receive token -
symbol : symbol of token to be transferred - amount : amount of token to
be transferred - memo: memo field in this transfer - to_chain_id :
destination chain id on which the tokens will be received -
issue_chain_id : the chain on which the token was issued

Receive on the destination chain
--------------------------------

On the destination chain tokens need to be received, it’s the
**CrossChainReceiveToken** method that is used to trigger the reception:

.. code:: protobuf

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

Let’s review the fields of the input: 

- **from_chain_id**: the source chain id on which cross chain transfer launched 

- **parent_chain_height**
  
  - for the case of transfer from main chain to side chain: this parent_chain_height is the height of the block on the main chain that contains the **CrossChainTransfer** transaction. 
  
  - for the case of transfer from side chain to side chain or side chain to main-chain: this **parent_chain_height** is the result of **GetBoundParentChainHeightAndMerklePathByHeight** (input is the height of the *CrossChainTransfer*, see :doc:`cross chain verification <./crosschain-verification>`) - accessible in the **bound_parent_chain_height** field. 

- **transfer_transaction_bytes**: the serialized form of the **CrossChainTransfer** transaction. 

- **merkle_path**
  
  - for the case of transfer from main chain to side chain: for this you just need the merkle path from the main chain’s web api with the **GetMerklePathByTransactionIdAsync** method (**CrossChainTransfer** transaction ID as input). 
  
  - for the case of transfer from side chain to side chain or from side chain to main chain: for this you also need to get the merkle path from the source node (side chain here). But you also have to complete this merkle path with **GetBoundParentChainHeightAndMerklePathByHeight** with the **CrossChainTransfer** transaction’s block height (concat the merkle path nodes). The nodes are in the **merkle_path_from_parent_chain** field of the **CrossChainMerkleProofContext** object.
