# Cross Chain Contract

## Functions

### Detailed Description

Defines C# API  functions for cross chain contract.

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

Initialize cross-chain-contract on parent-chain

**Parameters:**

***InitializeInput***

- **parent_chain_id** - id of parent-chain
- **creation_height_on_parent_chain** - creation height of side-chain on parent-chain

### function ChangeOwnerAddress

```protobuf
rpc ChangOwnerAddress(aelf.Address) returns (google.protobuf.Empty) {}
message Address
{
    bytes value = 1;
}
```

Change the address of owner of cross-chain-contract

**Parameters:**

- ***Address*** - the address owner wants to change to 

### function CreateSideChain

```protobuf
rpc CreateSideChain (SideChainCreationRequest) returns (aelf.SInt32Value) {}
message SideChainCreationRequest
{
    int64 indexing_price = 1;
    int64 locked_token_amount = 2;
    bytes contract_code = 3;
    bool is_privilege_preserved = 4;
    string side_chain_token_symbol = 5;
    string side_chain_token_name = 6;
    sint64 side_chain_token_total_supply = 7;
    sint32 side_chain_token_decimals = 8;
    bool is_side_chain_token_burnable = 9;
}
```

Create a side chain and the creation is a proposal result from system address.

**Parameters:**

***SideChainCreationRequest*** 

- **indexing_price** - price of indexing
- **locked_token_amount** - the initial balance of side-chain
- **contract_code** - codes of contract in byte
- **is_privilege_preserved** - ??
- **side_chain_token_symbol** - symbol of token on side-chain
- **side_chain_token_name** - name of token on side-chain
- **side_chain_token_total_supply** - total supply of token on side-chain
- **side_chain_token_decimals** - token's decimal on side-chain
- **is_side_chain_token_burnable** - whether the token can be burned

**Returns:**

Id of a new side-chain

### function Recharge

```protobuf
rpc Recharge (RechargeInput) returns (google.protobuf.Empty) {}
message RechargeInput {
    int32 chain_id = 1;
    sint64 amount = 2;
}
```

Recharge a certain amount of native symbols  for specified side-chain

**Parameters:**

***RechargeInput*** 

- **chain_id** - id of the side-chain
- **amount** - the amount of native symbols to recharge

### function RecordCrossChainData

```protobuf
rpc RecordCrossChainData (CrossChainBlockData) returns (google.protobuf.Empty) {}
message CrossChainBlockData {
    repeated SideChainBlockData side_chain_block_data = 1;
    repeated ParentChainBlockData parent_chain_block_data = 2;
    int64 previous_block_height = 3;
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
    aelf.Hash side_chain_block_headers_root = 1;
    aelf.Hash side_chain_transactions_root = 2;
}
```

Index block data of parent-chain and side-chain

**Parameters:**

***CrossChainBlockData***

- **SideChainBlockData**
  - height : height of side-chain
  - block_header_hash : hash of the block
  - transaction_merkle_tree_root : merkle tree root of transactions in the block
  - chain_id : id of side-chain
- **ParentChainBlockData**
  - height : height of parent-chain
  - **cross_chain_extra_data**
    - side_chain_block_headers_root : ??
    - side_chain_transactions_root : the merkle tree root recorded on parent-chain
  - chain_id : id of parent-chain
  - transaction_status_merkle_root : the merkle tree root made by transactionId and its status
  - indexed_merkle_path : 
  - extra_data : a map saving extra data of consensus
- **previous_block_height** - height of previous block

### function DisposeSideChain

```protobuf
rpc DisposeSideChain (aelf.SInt32Value) returns (aelf.SInt64Value) {}
message SInt32Value
{
    sint32 value = 1;
}
message SInt64Value
{
    sint64 value = 1;
}
```

Dispose the specified side-chain. It is a proposal result from system address.

**Parameters:**

- **SInt32Value** - the id of side chain which needs to be disposed

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

Verification of transaction.

**Parameters:**

***VerifyTransactionInput*** 

- **transaction_id** - hash of transaction
- **path** - merkle path of the transaction
- **parent_chain_height** - height of parent-chain
- **verified_chain_id** - id of the chain which is verified

**Returns:**

Whether the input transaction passed verification

### function CurrentSideChainSerialNumber

```protobuf
rpc CurrentSideChainSerialNumber (google.protobuf.Empty) returns (aelf.SInt64Value) 
{
		option (aelf.is_view) = true;
}
```

Get serial number of current side-chain

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Serial number of current side-chain

### function LockedToken

```protobuf
rpc LockedToken (aelf.SInt32Value) returns (aelf.SInt64Value) 
{
		option (aelf.is_view) = true;
}
```

Get amount of the locked token

**Parameters:**

- **SInt32Value** - id of side-chain

**Returns:**

Amount of the locked token

### function LockedAddress

```protobuf
rpc LockedAddress (aelf.SInt32Value) returns (aelf.Address)
{
		option (aelf.is_view) = true;
}
```

Get locked address

**Parameters:**

- **SInt32Value** - id of side-chain

**Returns:**

Address of side-chain's proposer

### function GetChainStatus

```protobuf
rpc GetChainStatus (aelf.SInt32Value) returns (aelf.SInt32Value)
{
		option (aelf.is_view) = true;
}
```

Get current status of the specified side-chain

**Parameters:**

- **SInt32Value** - id of side-chain

**Returns:**

Current status of the side-chain

### function GetSideChainHeight

```protobuf
rpc GetSideChainHeight (aelf.SInt32Value) returns (aelf.SInt64Value)
{
		option (aelf.is_view) = true;
}   
```

Get current height of the specified side-chain

**Parameters:**

- **SInt32Value** - id of side-chain

**Returns:**

Current height of the side chain

### function GetParentChainHeight

```protobuf
rpc GetParentChainHeight (google.protobuf.Empty) returns (aelf.SInt64Value)
{
		option (aelf.is_view) = true;
}
```

Get current height of parent-chain

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Height of parent-chain

### function GetParentChainId

```protobuf
rpc GetParentChainId (google.protobuf.Empty) returns (aelf.SInt32Value)
{
		option (aelf.is_view) = true;
}
```

Get id of the parent-chain which can't be zero

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

id of parent-chain

### function LockedBalance

```protobuf
rpc LockedBalance (aelf.SInt32Value) returns (aelf.SInt64Value) 
{
		option (aelf.is_view) = true;
}
```

Get balance of the specified side-chain

**Parameters:**

- **SInt32Value** - id of side-chain

**Returns:**

Balance of the side-chain

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

Get chain-id and height of side-chains

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

***SideChainIdAndHeightDict*** : A dictionary contains id and height of side-chains

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

Get information of indexed side-chains

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

***SideChainIndexingInformationList*** : A list contains information of indexed side-chains

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

Get chain-id and height of all chains

**Parameters:**

- **Empty** - empty input

**Returns:**

***SideChainIdAndHeightDict*** : A dictionary contains id and height of all-chains

### function GetIndexedCrossChainBlockDataByHeight

```protobuf
rpc GetIndexedCrossChainBlockDataByHeight (aelf.SInt64Value) returns (CrossChainBlockData)
{
		option (aelf.is_view) = true;
}
message CrossChainBlockData
{
    repeated SideChainBlockData side_chain_block_data = 1;
    repeated ParentChainBlockData parent_chain_block_data = 2;
    int64 previous_block_height = 3;
}
message SideChainBlockData 
{
    int64 height = 1;
    aelf.Hash block_header_hash = 2;
    aelf.Hash transaction_merkle_tree_root = 3;
    int32 chain_id = 4;
}
message ParentChainBlockData 
{
    int64 height = 1;
    CrossChainExtraData cross_chain_extra_data = 2;
    int32 chain_id = 3;
    aelf.Hash transaction_status_merkle_root = 4;
    map<int64, aelf.MerklePath> indexed_merkle_path = 5;
    map<string, bytes> extra_data = 6;
}
message CrossChainExtraData
{
    aelf.Hash side_chain_block_headers_root = 1;
    aelf.Hash side_chain_transactions_root = 2;
}
```

Get block data of indexed cross-chain by height

**Parameters:**

- **SInt64Value** - height of chain

**Returns:**

***CrossChainBlockData***

- **side_chain_block_data** - block data of side-chain
- **parent_chain_block_data** - block data of main-chain
- **previous_block_height** - height of previous block

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

Get block data of indexed side-chain by height

**Parameters:**

- **SInt64Value** - height of side-chain

**Returns:**

***IndexedSideChainBlockData***

- **side_chain_block_data** - block data of side-chain

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

Get merkle proof of parent-chain which is bound up with side-chain

**Parameters:**

- **SInt64Value** - height of side-chain

**Returns:**

***CrossChainMerkleProofContext***

- **bound_parent_chain_height** - height of parent-chain bound up with side-chain
- **merkle_path_for_parent_chain_root** - ??

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

Get initialization data of the specified side-chain

**Parameters:**

- **Int32Value** - id of side-chain

**Returns:**

***ChainInitializationData***

- **chain_id** - id of side-chain
- **creator** - proposer of side-chain
- **creation_timestamp** - timestamp for side-chain creation
- **extra_information** - extra infomation like consensus and nativeToken etc.
- **creation_height_on_parent_chain** - creation height of side-chain on parent-chain
- **chain_creator_privilege_preserved** - ??
- **side_chain_token_symbol** - token symbol of side-chain