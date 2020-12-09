# Cross Chain Contract

Defines C# API functions for cross chain contract.

## **Actions**

### ProposeCrossChainIndexing

```protobuf
rpc ProposeCrossChainIndexing(CrossChainBlockData) returns (google.protobuf.Empty) {}

message CrossChainBlockData {
    repeated SideChainBlockData side_chain_block_data_list = 1;
    repeated ParentChainBlockData parent_chain_block_data_list = 2;
}

message SideChainBlockData {
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_status_merkle_tree_root = 3;
    int32 chain_id = 4;
}

message ParentChainBlockData {
    int64 height = 1;
    CrossChainExtraData cross_chain_extra_data = 2;
    int32 chain_id = 3;
    aelf.Hash transaction_status_merkle_tree_root = 4;
    map<int64, aelf.MerklePath> indexed_merkle_path = 5;
    map<string, bytes> extra_data = 6;
}

message CrossChainExtraData {
    aelf.Hash transaction_status_merkle_tree_root = 1;
}

message ProposalCreated{
    option (aelf.is_event) = true;
    aelf.Hash proposal_id = 1;
}
```

Propose once cross chain indexing.

- **CrossChainBlockData**
  - **side_chain_block_data_list**: side chain block data list.
  - **parent_chain_block_data_list**: parent chain block data list.

- **SideChainBlockData**
  - **height**: height of side chain block.
  - **block_header_hash**: hash of side chain block.
  - **transaction_merkle_tree_root**: merkle tree root computing from transactions status in side chain block.
  - **chain_id**: id of side chain.

- **ParentChainBlockData**
  - **height**: height of parent chain.
  - **cross_chain_extra_data**: the merkle tree root computing from side chain roots.
  - **chain_id**: parent chain id.
  - **transaction_status_merkle_root**: merkle tree root computing from transactions status in parent chain block.
  - **indexed_merkle_path**: \<block height, merkle path> key-value map.
  - **extra_data**: extra data map.

- **CrossChainExtraData**
  - **transaction_status_merkle_tree_root**: the hash value of merkle tree root.

### ReleaseCrossChainIndexing

```protobuf
rpc ReleaseCrossChainIndexing(ReleaseCrossChainIndexingProposalInput) returns (google.protobuf.Empty) {
  
message ReleaseCrossChainIndexingProposalInput{
    repeated int32 chain_id_list = 1;
}
```

Release cross chain indexing proposal.

- **ReleaseCrossChainIndexingProposalInput**
  - **chain_id_list**: chain id list to release

### Initialize

```protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {}

message InitializeInput
{
    int32 parent_chain_id = 1;
    int64 creation_height_on_parent_chain = 2;
    bool is_privilege_preserved = 3;
}
```

Initialize cross-chain-contract.

- **InitializeInput**
  - **parent_chain_id**: id of parent chain
  - **creation_height_on_parent_chain**: height of side chain creation on parent chain
  - **is_privilege_preserved**: true if chain privilege needed, otherwise false 

### RequestSideChainCreation

```protobuf
rpc RequestSideChainCreation(SideChainCreationRequest) returns (google.protobuf.Empty){}

message SideChainCreationRequest {
    int64 indexing_price = 1;
    int64 locked_token_amount = 2;
    bool is_privilege_preserved = 3;
    SideChainTokenCreationRequest side_chain_token_creation_request = 4;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 5;
    map<string, int32> initial_resource_amount = 6;
}

message SideChainTokenCreationRequest{
    string side_chain_token_symbol = 1;
    string side_chain_token_name = 2;
    int64 side_chain_token_total_supply = 3;
    int32 side_chain_token_decimals = 4;
    bool is_side_chain_token_burnable = 5;
}

message SideChainTokenInitialIssue{
    aelf.Address address = 1;
    int64 amount = 2;
}
```

Request side chain creation.

- **SideChainCreationRequest**
  - **indexing_price**: indexing fee.
  - **locked_token_amount**: initial locked balance for a new side chain.
  - **is_privilege_preserved**: creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
  - **side_chain_token_initial_issue_list**: a list of accounts and amounts that will be issued when the chain starts.
  - **initial_resource_amount**: the initial rent resources.
  
- **SideChainTokenCreationRequest**
  - **side_chain_token_symbol**: side chain token symbol.
  - **side_chain_token_name**: side chain token name.
  - **side_chain_token_total_supply**:  total supply of side chain token.
  - **side_chain_token_decimals**: side chain token decimal.
  


### ReleaseSideChainCreation

```protobuf
rpc ReleaseSideChainCreation(ReleaseSideChainCreationInput) returns (google.protobuf.Empty){}

message ReleaseSideChainCreationInput {
    aelf.Hash proposal_id = 1;
}
```

Release side chain creation proposal and side chain will be created.

- **ReleaseSideChainCreationInput**
  - **proposal_id**: side chain creation proposal id

### CreateSideChain

```protobuf
rpc CreateSideChain (CreateSideChainInput) returns (google.protobuf.Int32Value) {}

message CreateSideChainInput{
    SideChainCreationRequest side_chain_creation_request = 1;
    aelf.Address proposer = 2;
}

message SideChainCreationRequest {
    int64 indexing_price = 1;
    int64 locked_token_amount = 2;
    bool is_privilege_preserved = 3;
    string side_chain_token_symbol = 4;
    string side_chain_token_name = 5;
    int64 side_chain_token_total_supply = 6;
    int32 side_chain_token_decimals = 7;
    bool is_side_chain_token_burnable = 8;
    bool is_side_chain_token_profitable = 9;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 10;
    map<string, int32> initial_resource_amount = 11;
}

message SideChainTokenInitialIssue {
    aelf.Address address = 1;
    int64 amount = 2;
}
```

Create a new side chain, this is be triggered by an organization address. 

- **CreateSideChainInput**
  - **proposer**: the proposer of the proposal that triggered this method.
  - **SideChainCreationRequest**: the parameters of creating side chain.

note: *for SideChainCreationRequest see RequestSideChainCreation*

- **Returns**
Id of a new side chain

### SetInitialSideChainLifetimeControllerAddress

```protobuf
rpc SetInitialSideChainLifetimeControllerAddress(aelf.Address) returns (google.protobuf.Empty){}

```

Sets the initial **SideChainLifetimeController** address which should be parliament organization by default.

- **Address** : the owner's address.

### SetInitialIndexingControllerAddress

```protobuf
rpc SetInitialIndexingControllerAddress(aelf.Address) returns (google.protobuf.Empty){}

```

Sets the initial **CrossChainIndexingController** address which should be parliament organization by default.

- **Address**: the owner's address.

### ChangeCrossChainIndexingController

```protobuf
rpc ChangeCrossChainIndexingController(AuthorityInfo) returns (google.protobuf.Empty) { }

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Changes the cross chain indexing controller.

- **AuthorityInfo**
  - **contract_address**: the address of the contract that generated the controller.
  - **owner_address**: the address of the controller.

### ChangeSideChainLifetimeController

```protobuf
rpc ChangeSideChainLifetimeController(AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Changes the side chain's lifetime controller.

note: *for AuthorityInfo see ChangeCrossChainIndexingController*


### ChangeSideChainIndexingFeeController

```protobuf
rpc ChangeSideChainIndexingFeeController(ChangeSideChainIndexingFeeControllerInput) returns (google.protobuf.Empty){}

message ChangeSideChainIndexingFeeControllerInput{
    int32 chain_id = 1;
    AuthorityInfo authority_info = 2;
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Changes indexing fee adjustment controller for specific side chain.

- **ChangeSideChainIndexingFeeControllerInput**
  - **chain_id**: side chain id.
  - **authority_info**
    - **contract_address**: the address of the contract that generated the controller.
    - **owner_address**: the address of the controller.


### Recharge

```protobuf
rpc Recharge (RechargeInput) returns (google.protobuf.Empty) {}
message RechargeInput {
    int32 chain_id = 1;
    int64 amount = 2;
}
```

Recharge for specified side chain.

- **RechargeInput**
  - **chain_id**: id of the side chain
  - **amount**: the token amount to recharge

### RecordCrossChainData

```protobuf
rpc RecordCrossChainData (RecordCrossChainDataInput) returns (google.protobuf.Empty){}

message RecordCrossChainDataInput{
    CrossChainBlockData proposed_cross_chain_data = 1;
    aelf.Address proposer = 2;
}

message CrossChainBlockData {
    repeated SideChainBlockData side_chain_block_data_list = 1;
    repeated ParentChainBlockData parent_chain_block_data_list = 2;
    int64 previous_block_height = 3;
}

message SideChainBlockData {
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_status_merkle_tree_root = 3;
    int32 chain_id = 4;
}

message ParentChainBlockData {
    int64 height = 1;
    CrossChainExtraData cross_chain_extra_data = 2;
    int32 chain_id = 3;
    aelf.Hash transaction_status_merkle_tree_root = 4;
    map<int64, aelf.MerklePath> indexed_merkle_path = 5;
    map<string, bytes> extra_data = 6;
}

message CrossChainExtraData {
    aelf.Hash transaction_status_merkle_tree_root = 1;
}
```

Index block data of parent chain and side chain. Only **CrossChainIndexingController** is permitted to invoke this method.

- **RecordCrossChainDataInput**
  - **CrossChainBlockData**: cross chain block data.
  - **proposer**: the proposal's address.

note: *for CrossChainBlockData see ProposeCrossChainIndexing*

### AdjustIndexingFeePrice

```protobuf
rpc AdjustIndexingFeePrice(AdjustIndexingFeeInput)returns(google.protobuf.Empty){}

message AdjustIndexingFeeInput{
    int32 side_chain_id = 1;
    int64 indexing_fee = 2;
}
```

Adjust side chain indexing fee. Only **IndexingFeeController** is permitted to invoke this method.

- **AdjustIndexingFeeInput**
- **side_chain_id**: side chain id
- **indexing_fee**: indexing fee to be set

### DisposeSideChain

```protobuf
rpc DisposeSideChain (google.protobuf.Int32Value) returns (google.protobuf.Int32Value){}
```

Dispose the specified side chain. Only **SideChainLifetimeController** is permitted to invoke this method.

- **Int32Value**: the id of side chain to be disposed

- **Returns**
  - **value**: the id of disposed chain

## **View methods**

Get pending cross chain indexing proposal info.

- **Returns**
  - **proposal_id**: cross chain indexing proposal id.
  - **proposer**: proposer of cross chain indexing proposal.
  - **to_be_released**: true if the proposal can be released, otherwise false.
  - **proposed_cross_chain_block_data**: cross chain data proposed.
  - **expired_time**: proposal expiration time.

note: *for CrossChainBlockData see ProposeCrossChainIndexing*


### GetCrossChainIndexingController

```protobuf
rpc GetCrossChainIndexingController(google.protobuf.Empty) returns (AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Get indexing fee adjustment controller for specific side chain.

note: *for AuthorityInfo see ChangeCrossChainIndexingController*


### GetSideChainLifetimeController

```protobuf
rpc GetSideChainLifetimeController(google.protobuf.Empty) returns (AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Get the side chain's lifetime controller.

note: *for AuthorityInfo see ChangeCrossChainIndexingController*.

### GetSideChainIndexingFeeController

```protobuf
rpc GetSideChainIndexingFeeController(google.protobuf.Int32Value) returns (AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Get side chain indexing fee.

- **Int32Value**: side chain id

note: *for AuthorityInfo see ChangeCrossChainIndexingController*

### GetSideChainIndexingFeePrice

```protobuf
rpc GetSideChainIndexingFeePrice(google.protobuf.Int32Value) returns (google.protobuf.Int64Value){}

```

Get side chain indexing fee.

- **Int32Value**: side chain id

- **Returns**
  - **value**: indexing fee price


### VerifyTransaction

```protobuf
rpc VerifyTransaction (VerifyTransactionInput) returns (google.protobuf.BoolValue){}

message VerifyTransactionInput {
    aelf.Hash transaction_id = 1;
    aelf.MerklePath path = 2;
    int64 parent_chain_height = 3;
    int32 verified_chain_id = 4;
}
```

Transaction cross chain verification.

- **VerifyTransactionInput**
  - **transaction_id**: transaction id
  - **path**: merkle path for the transaction
  - **parent_chain_height**: height of parent chain indexing this transaction
  - **verified_chain_id**: id of the chain to be verified

- **Returns**
  - **value**: true if verification succeeded, otherwise false.

### LockedAddress

```protobuf
rpc GetSideChainCreator (google.protobuf.Int32Value) returns (aelf.Address){}
```

Get side chain creator address.

- **Int32Value**: id of side chain

- **Returns**
  - **value**: address of side chain creator.

### GetChainStatus

```protobuf
rpc GetChainStatus (google.protobuf.Int32Value) returns (GetChainStatusOutput){}

message GetChainStatusOutput{
    SideChainStatus status = 1;
}

enum SideChainStatus
{
    FATAL = 0;
    ACTIVE = 1;
    INDEXING_FEE_DEBT = 2;
    TERMINATED = 3;
}
```

Gets the current status of the specified side chain.

- **Int32Value**: id of side chain.

- **Returns**
Current status of side chain.
  - **fatal**: currently no meaning.
  - **active**: the side-chain is being indexed.
  - **insufficient fee debt**: debt for indexing fee to be payed off.
  - **terminated**: the side chain cannot be indexed anymore.

### GetSideChainHeight

```protobuf
rpc GetSideChainHeight (google.protobuf.Int32Value) returns (google.protobuf.Int64Value){}
```

Get current height of the specified side chain.

- **Int32Value**: id of side chain

- **Returns**
  - **value**: current height of the side chain.

### GetParentChainHeight

```protobuf
rpc GetParentChainHeight (google.protobuf.Empty) returns (google.protobuf.Int64Value){}
```

Get recorded height of parent chain

- **Returns**
  - **value**: height of parent chain.

### GetParentChainId

```protobuf
rpc GetParentChainId (google.protobuf.Empty) returns (google.protobuf.Int32Value){}
```

Get id of the parent chain. This interface is only for side chain.

- **Returns**
  - **value**: parent chain id.

### GetSideChainBalance

```protobuf
rpc GetSideChainBalance (google.protobuf.Int32Value) returns (google.protobuf.Int64Value){}
```

Get the balance for side chain indexing.

- **Int32Value**: id of side chain

- **Returns**
  - **value**: balance for side chain indexing.

### GetSideChainIndexingFeeDebt

```protobuf
rpc GetSideChainIndexingFeeDebt (google.protobuf.Int32Value) returns (google.protobuf.Int64Value){}
```

Get indexing debt for side chain.

- **Int32Value**: id of side chain

- **Returns**
  - **value**: side chain indexing debt. Returns zero if no debt.

### GetSideChainIdAndHeight

```protobuf
rpc GetSideChainIdAndHeight (google.protobuf.Empty) returns (ChainIdAndHeightDict){}

message ChainIdAndHeightDict
{
    map<int32, int64> id_height_dict = 1;
}
```

Get id and recorded height of side chains.

- **Returns**
  - **ChainIdAndHeightDict**: A map contains id and height of side chains

### GetSideChainIndexingInformationList

```protobuf
rpc GetSideChainIndexingInformationList (google.protobuf.Empty) returns (SideChainIndexingInformationList){}

message SideChainIndexingInformationList
{
    repeated SideChainIndexingInformation indexing_information_list = 1;
}

message SideChainIndexingInformation
{
    int32 chain_id = 1;
    int64 indexed_height = 2;
}
```

Get indexing information of side chains.

- **Returns**
  - **SideChainIndexingInformationList**: A list contains indexing information of side chains

### GetAllChainsIdAndHeight

```protobuf
rpc GetAllChainsIdAndHeight (google.protobuf.Empty) returns (ChainIdAndHeightDict){}

message ChainIdAndHeightDict
{
    map<int32, int64> id_height_dict = 1;
}
```

Get id and recorded height of all chains.

- **Returns**
  -**ChainIdAndHeightDict**: A map contains id and height of all chains


### GetIndexedSideChainBlockDataByHeight

```protobuf
rpc GetIndexedSideChainBlockDataByHeight (google.protobuf.Int64Value) returns (IndexedSideChainBlockData){}

message IndexedSideChainBlockData
{
    repeated acs7.SideChainBlockData side_chain_block_data = 1;
}
message SideChainBlockData
{
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_merkle_tree_root = 3;
    int32 chain_id = 4;
}
```

Get block data of indexed side chain by height

- **Int64Value**: height of side chain

- **Returns**
  - **side_chain_block_data**: block data of side chain.

### GetBoundParentChainHeightAndMerklePathByHeight

```protobuf
rpc GetBoundParentChainHeightAndMerklePathByHeight (google.protobuf.Int64Value) returns (CrossChainMerkleProofContext){}

message CrossChainMerkleProofContext
{
    int64 bound_parent_chain_height = 1;
    aelf.MerklePath merkle_path_for_parent_chain_root = 2;
}
message MerklePath
{
    repeated MerklePathNode merklePathNodes = 1;
}
message MerklePathNode
{
    Hash hash = 1;
    bool isLeftChildNode = 2;
}
```

Get merkle path bound up with side chain

- **Int64Value**: height of side chain.

- **Returns**
  - **bound_parent_chain_height**: height of parent chain bound up with side chain.
  - **merkle_path_from_parent_chain**: merkle path generated from parent chain.

### GetChainInitializationData

```protobuf
rpc GetChainInitializationData (google.protobuf.Int32Value) returns (ChainInitializationData){}

message ChainInitializationData
{
    int32 chain_id = 1;
    aelf.Address creator = 2;
    google.protobuf.Timestamp creation_timestamp = 3;
    int64 creation_height_on_parent_chain = 4;
    bool chain_creator_privilege_preserved = 5;
    aelf.Address parent_chain_token_contract_address = 6;
    ChainInitializationConsensusInfo chain_initialization_consensus_info = 7;
    bytes native_token_info_data = 8;
    ResourceTokenInfo resource_token_info = 9;
    ChainPrimaryTokenInfo chain_primary_token_info = 10;
}

message ChainInitializationConsensusInfo{
    bytes initial_miner_list_data = 1;
}

message ResourceTokenInfo{
    bytes resource_token_list_data = 1;
    map<string, int32> initial_resource_amount = 2;
}

message ChainPrimaryTokenInfo{
    bytes chain_primary_token_data = 1;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 2;
}

message SideChainTokenInitialIssue{
    aelf.Address address = 1;
    int64 amount = 2;
}
```

Get initialization data for specified side chain.

- **Int32Value**: id of side chain.

- **Returns**
  - **chain_id**: id of side chain.
  - **creator**: side chain creator.
  - **creation_timestamp**: timestamp for side chain creation.
  - **creation_height_on_parent_chain**: height of side chain creation on parent chain.
  - **chain_creator_privilege_preserved**: creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
  - **parent_chain_token_contract_address**: parent chain token contract address.
  - **chain_initialization_consensus_info**: initial miner list information.
  - **native_token_info_data**: native token info.
  - **resource_token_info**: resource token information.
  - **chain_primary_token_info**: chain priamry token information.

- **ChainInitializationConsensusInfo**
  - **initial_consensus_data**: initial consensus data.

- **ChainInitializationConsensusInfo**
  - **resource_token_list_data**: resource token list data.
  - **initial_resource_amount**: initial resource token amount.

- **ChainPrimaryTokenInfo**
  - **chain_primary_token_data**: side chain primary token data.
  - **side_chain_token_initial_issue_list**: side chain primary token initial issue list.
