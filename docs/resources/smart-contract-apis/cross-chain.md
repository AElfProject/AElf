# Cross Chain Contract

## Functions

### Detailed Description

Defines C# API functions for cross chain contract.

## Functions Documentation

### function Initialize

```protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {}
message InitializeInput 
{
    int32 parent_chain_id = 1;
    int64 creation_height_on_parent_chain = 2;
}
```

Initialize cross-chain-contract.

**Parameters:**

***InitializeInput***

- **parent_chain_id** - id of parent chain
- **creation_height_on_parent_chain** - height of side chain creation on parent chain

### function CreateSideChain

```protobuf
rpc CreateSideChain (CreateSideChainInput) returns (aelf.SInt32Value) {}

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
    sint64 side_chain_token_total_supply = 6;
    sint32 side_chain_token_decimals = 7;
    bool is_side_chain_token_burnable = 8;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 9;
    map<string, sint32> initial_resource_amount = 10; 
    bool is_side_chain_token_profitable = 11;
}

message SideChainTokenInitialIssue {
    aelf.Address address = 1;
    int64 amount = 2;
}
```

Create a new side chain, this is be triggered by an organization address. 

**Parameters:**

- **CreateSideChainInput**
  - **proposer** the proposer of the proposal that triggered this method.
  - ***SideChainCreationRequest*** 
    - **indexing_price** - indexing fee.
    - **locked_token_amount** - initial locked balance for a new side chain.
    - **is_privilege_preserved** - creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
    - **side_chain_token_symbol** - side chain token symbol.
    - **side_chain_token_name** - side chain token name.
    - **side_chain_token_total_supply** -  total supply of side chain token.
    - **side_chain_token_decimals** - sÏide chain token decimal.
    - **is_side_chain_token_burnable** - side chain token burnable flag.
    - **side_chain_token_initial_issue_list** - a list of accounts and amounts that will be issued when the chain starts.
    - **initial_resource_amount** - the initial rent resources.
    - **is_side_chain_token_profitable** - a flag to indicate wether the chain is profitable or not.

**Returns:**

Id of a new side chain

### function SetInitialControllerAddress

```protobuf
rpc SetInitialControllerAddress(aelf.Address) returns (google.protobuf.Empty) { }
```

Sets the initial controller address.

**Parameters:**
- **address** : the owner's address.

### function ChangeCrossChainIndexingController

```protobuf
rpc ChangeCrossChainIndexingController(acs1.AuthorityInfo) returns (google.protobuf.Empty) { }

message acs1.AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Changes the indexing controller.

**Parameters:**
- **acs1.AuthorityInfo** : 
  - contract_address - the address of the contract that generated the owner address.
  - owner_address - the address of the owner that was generated.

### function ChangeSideChainLifetimeController

```protobuf
rpc ChangeSideChainLifetimeController(acs1.AuthorityInfo) returns (google.protobuf.Empty) { }

message acs1.AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Changes the side chain's lifetime controller.

**Parameters:**
- **acs1.AuthorityInfo** : 
  - contract_address - the address of the contract that generated the owner address.
  - owner_address - the address of the owner that was generated.

### function Recharge

```protobuf
rpc Recharge (RechargeInput) returns (google.protobuf.Empty) {}
message RechargeInput {
    int32 chain_id = 1;
    sint64 amount = 2;
}
```

Recharge for specified side chain.

**Parameters:**

***RechargeInput*** 

- **chain_id** - id of the side chain
- **amount** - the token amount to recharge

### function RecordCrossChainData

```protobuf
rpc RecordCrossChainData (CrossChainBlockData) returns (google.protobuf.Empty) {}
message CrossChainBlockData {
    repeated SideChainBlockData side_chain_block_data = 1;
    repeated ParentChainBlockData parent_chain_block_data = 2;
}
message SideChainBlockData {
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_merkle_tree_root = 3;
    int32 chain_id = 4;
}
message ParentChainBlockData {
    int64 height = 1;
    CrossChainExtraData cross_chain_extra_data = 2;
    int32 chain_id = 3;
    aelf.Hash transaction_status_merkle_root = 4;
    map<int64, aelf.MerklePath> indexed_merkle_path = 5;
    map<string, bytes> extra_data = 6;
}
message CrossChainExtraData {
    aelf.Hash transaction_status_merkle_tree_root = 1;
}
```

Index block data of parent chain and side chain. Only current block generator is permitted to invoke this method and it would be system transaction automatically generated during block mining.

**Parameters:**

***CrossChainBlockData***

- **SideChainBlockData**
  - height : height of side chain block
  - block_header_hash : hash of side chain block
  - transaction_merkle_tree_root : merkle tree root computing from transactions status in side chain block
  - chain_id : id of side chain

- **ParentChainBlockData**
  - height : height of parent chain
  - **cross_chain_extra_data** 
    - transaction_status_merkle_tree_root : the merkle tree root computing from side chain roots.
  - chain_id : parent chain id
  - transaction_status_merkle_root : merkle tree root computing from transactions status in parent chain block
  - indexed_merkle_path : <block height, merkle path> key-value map
  - extra_data : extra data map


### function DisposeSideChain

```protobuf
rpc DisposeSideChain (aelf.SInt32Value) returns (aelf.SInt64Value) {}
```

Dispose the specified side chain. Only contract owner is permitted to invoke this method.

**Parameters:**

- **SInt32Value** - the id of side chain to be disposed

**Returns:**

the id of disposed chain

### function VerifyTransaction

```protobuf
rpc VerifyTransaction (VerifyTransactionInput) returns (google.protobuf.BoolValue) 
{
    option (aelf.is_view) = true;    
}
message VerifyTransactionInput {
    aelf.Hash transaction_id = 1;
    aelf.MerklePath path = 2;
    sint64 parent_chain_height = 3;
    int32 verified_chain_id = 4;
}
```

Transaction cross chain verification.

**Parameters:**

***VerifyTransactionInput*** 

- **transaction_id** - transaction id
- **path** - merkle path for the transaction
- **parent_chain_height** - height of parent chain indexing this transaction
- **verified_chain_id** - id of the chain to be verified

**Returns:**

True if verification succeeded, otherwise false.


### function LockedAddress

```protobuf
rpc GetSideChainCreator (aelf.SInt32Value) returns (aelf.Address)
{
    option (aelf.is_view) = true;
}
```

Get side chain creator address.

**Parameters:**

- **SInt32Value** - id of side chain

**Returns:**

Address of side chain creator.

### function GetChainStatus

```protobuf
rpc GetChainStatus (aelf.SInt32Value) returns (GetChainStatusOutput)
{
    option (aelf.is_view) = true;
}

message GetChainStatusOutput{
    SideChainStatus status = 1;
}

enum SideChainStatus
{
    FATAL = 0;
    ACTIVE = 1;
    INSUFFICIENT_BALANCE = 2;
    TERMINATED = 3;
}
```

Gets the current status of the specified side chain.

**Parameters:**

- **SInt32Value** - id of side chain.

**Returns:**

Current status of side chain.
- fatal: currently no meaning.
- active: the side-chain is being indexed.
- insufficient balance: not enough balance for indexing.
- terminated: the side chain cannot be indexed anymore.

### function GetSideChainHeight

```protobuf
rpc GetSideChainHeight (aelf.SInt32Value) returns (aelf.SInt64Value)
{
    option (aelf.is_view) = true;
}   
```

Get current height of the specified side chain.

**Parameters:**

- **SInt32Value** - id of side chain

**Returns:**

Current height of the side chain.

### function GetParentChainHeight

```protobuf
rpc GetParentChainHeight (google.protobuf.Empty) returns (aelf.SInt64Value)
{
    option (aelf.is_view) = true;
}
```

Get recorded height of parent chain

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Height of parent chain.

### function GetParentChainId

```protobuf
rpc GetParentChainId (google.protobuf.Empty) returns (aelf.SInt32Value)
{
    option (aelf.is_view) = true;
}
```

Get id of the parent chain which can't be zero

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Parent chain id.

### function LockedBalance

```protobuf
rpc GetSideChainBalance (aelf.SInt32Value) returns (aelf.SInt64Value) 
{
    option (aelf.is_view) = true;
}
```

Get locked token balance for side chain.

**Parameters:**

- **SInt32Value** - id of side chain

**Returns:**

Balance of the side chain

### function GetSideChainIdAndHeight

```protobuf
rpc GetSideChainIdAndHeight (google.protobuf.Empty) returns (SideChainIdAndHeightDict) 
{
    option (aelf.is_view) = true;
}
message SideChainIdAndHeightDict 
{
    map<int32, int64> id_height_dict = 1;
}
```

Get id and recorded height of side chains.

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

***SideChainIdAndHeightDict*** : A map contains id and height of side chains

### function GetSideChainIndexingInformationList

```protobuf
rpc GetSideChainIndexingInformationList (google.protobuf.Empty) returns (SideChainIndexingInformationList) 
{
    option (aelf.is_view) = true;
}
message SideChainIndexingInformationList
{
    repeated SideChainIndexingInformation indexing_information_list = 1;
}
message SideChainIndexingInformation
{
    int32 chain_id = 1;
    int64 indexed_height = 2;
    int64 to_be_indexed_count = 3;
}
```

Get indexing information of side chains.

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

***SideChainIndexingInformationList*** : A list contains indexing information of side chains


### function GetAllChainsIdAndHeight

```protobuf
rpc GetAllChainsIdAndHeight (google.protobuf.Empty) returns (SideChainIdAndHeightDict) 
{
    option (aelf.is_view) = true;
}
message SideChainIdAndHeightDict 
{
    map<int32, int64> id_height_dict = 1;
}
```

Get id and recorded height of all chains.

**Parameters:**

- **Empty** - empty input

**Returns:**

***SideChainIdAndHeightDict*** : A map contains id and height of all chains



### function GetIndexedCrossChainBlockDataByHeight

```protobuf
rpc GetIndexedCrossChainBlockDataByHeight (aelf.SInt64Value) returns (CrossChainBlockData)
{
    option (aelf.is_view) = true;
}
message CrossChainBlockData {
    repeated SideChainBlockData side_chain_block_data = 1;
    repeated ParentChainBlockData parent_chain_block_data = 2;
}
message SideChainBlockData {
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_merkle_tree_root = 3;
    int32 chain_id = 4;
}
message ParentChainBlockData {
    int64 height = 1;
    CrossChainExtraData cross_chain_extra_data = 2;
    int32 chain_id = 3;
    aelf.Hash transaction_status_merkle_root = 4;
    map<int64, aelf.MerklePath> indexed_merkle_path = 5;
    map<string, bytes> extra_data = 6;
}
message CrossChainExtraData {
    aelf.Hash transaction_status_merkle_tree_root = 1;
}
```

Get indexed cross chain data by height.

**Parameters:**

- **SInt64Value** - block height

**Returns:**

***CrossChainBlockData***

- **side_chain_block_data** - cross chain block data of side chain
- **parent_chain_block_data** - cross chain block data of parent chain

### function GetIndexedSideChainBlockDataByHeight

```protobuf
rpc GetIndexedSideChainBlockDataByHeight (aelf.SInt64Value) returns (IndexedSideChainBlockData) {
    option (aelf.is_view) = true;
}
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

**Parameters:**

- **SInt64Value** - height of side chain

**Returns:**

***IndexedSideChainBlockData***

- **side_chain_block_data** - block data of side chain

### function GetBoundParentChainHeightAndMerklePathByHeight

```protobuf
rpc GetBoundParentChainHeightAndMerklePathByHeight (aelf.SInt64Value) returns (CrossChainMerkleProofContext) 
{
    option (aelf.is_view) = true;
}
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

**Parameters:**

- **SInt64Value** - height of side chain

**Returns:**

***CrossChainMerkleProofContext***

- **bound_parent_chain_height** - height of parent chain bound up with side chain
- **merkle_path_from_parent_chain** - merkle path generated from parent chain

### function GetChainInitializationData

```protobuf
rpc GetChainInitializationData (aelf.SInt32Value) returns (ChainInitializationData) 
{
    option (aelf.is_view) = true;
}
message ChainInitializationData 
{
    int32 chain_id = 1;
    aelf.Address creator = 2;
    google.protobuf.Timestamp creation_timestamp = 3;
    repeated bytes extra_information = 4;
    int64 creation_height_on_parent_chain = 5;
    bool chain_creator_privilege_preserved = 6;
    string side_chain_token_symbol = 7;
}
```

Get initialization data for specified side chain.

**Parameters:**

- **Int32Value** - id of side chain

**Returns:**

***ChainInitializationData***

- **chain_id** - id of side chain
- **creator** - side chain creator
- **creation_timestamp** - timestamp for side chain creation
- **extra_information** - extra infomation like consensus and nativeToken etc.
- **creation_height_on_parent_chain** - height of side chain creation on parent chain
- **chain_creator_privilege_preserved** - creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
- **side_chain_token_symbol** - token symbol of side chain