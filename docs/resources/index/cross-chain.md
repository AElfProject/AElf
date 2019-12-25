# Cross chain Contract

## Functions

### Detailed Description

Defines C\# API functions for cross chain contract.

## Functions Documentation

### function Initialize

```text
rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {}
message InitializeInput 
{
    int32 parent_chain_id = 1;
    int64 creation_height_on_parent_chain = 2;
}
```

Initialize cross-chain-contract.

**Parameters:**

_**InitializeInput**_

* **parent\_chain\_id** - id of parent chain
* **creation\_height\_on\_parent\_chain** - height of side chain creation on parent chain

### function ChangeOwnerAddress

```text
rpc ChangOwnerAddress(aelf.Address) returns (google.protobuf.Empty) {}
```

Change the owner address of cross-chain-contract. Only origin owner is permitted to invoke this method to change to new address.

**Parameters:**

* _**Address**_ - new contract owner address

### function CreateSideChain

```text
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

Create a new side chain. Only contract owner is permitted to invoke this method.

**Parameters:**

_**SideChainCreationRequest**_

* **indexing\_price** - indexing fee.
* **locked\_token\_amount** - initial locked balance for a new side chain.
* **is\_privilege\_preserved** - creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
* **side\_chain\_token\_symbol** - side chain token symbol.
* **side\_chain\_token\_name** - side chain token name.
* **side\_chain\_token\_total\_supply** -  total supply of side chain token.
* **side\_chain\_token\_decimals** - sÏide chain token decimal.
* **is\_side\_chain\_token\_burnable** - side chain token burnable flag.

**Returns:**

Id of a new side chain

### function Recharge

```text
rpc Recharge (RechargeInput) returns (google.protobuf.Empty) {}
message RechargeInput {
    int32 chain_id = 1;
    sint64 amount = 2;
}
```

Recharge for specified side chain.

**Parameters:**

_**RechargeInput**_

* **chain\_id** - id of the side chain
* **amount** - the token amount to recharge

### function RecordCrossChainData

```text
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

_**CrossChainBlockData**_

* **SideChainBlockData**
  * height : height of side chain block
  * block\_header\_hash : hash of side chain block
  * transaction\_merkle\_tree\_root : merkle tree root computing from transactions status in side chain block
  * chain\_id : id of side chain
* **ParentChainBlockData**
  * height : height of parent chain
  * **cross\_chain\_extra\_data** 
    * transaction\_status\_merkle\_tree\_root : the merkle tree root computing from side chain roots.
  * chain\_id : parent chain id
  * transaction\_status\_merkle\_root : merkle tree root computing from transactions status in parent chain block
  * indexed\_merkle\_path :  key-value map
  * extra\_data : extra data map

### function DisposeSideChain

```text
rpc DisposeSideChain (aelf.SInt32Value) returns (aelf.SInt64Value) {}
```

Dispose the specified side chain. Only contract owner is permitted to invoke this method.

**Parameters:**

* **SInt32Value** - the id of side chain to be disposed

**Returns:**

the id of disposed chain

### function VerifyTransaction

```text
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

_**VerifyTransactionInput**_

* **transaction\_id** - transaction id
* **path** - merkle path for the transaction
* **parent\_chain\_height** - height of parent chain indexing this transaction
* **verified\_chain\_id** - id of the chain to be verified

**Returns:**

True if verification succeeded, otherwise false.

### function LockedAddress

```text
rpc GetSideChainCreator (aelf.SInt32Value) returns (aelf.Address)
{
        option (aelf.is_view) = true;
}
```

Get side chain creator address.

**Parameters:**

* **SInt32Value** - id of side chain

**Returns:**

Address of side chain creator.

### function GetChainStatus

```text
rpc GetChainStatus (aelf.SInt32Value) returns (aelf.SInt32Value)
{
        option (aelf.is_view) = true;
}
```

Get current status of the specified side chain

**Parameters:**

* **SInt32Value** - id of side chain

**Returns:**

Current status of side chain

### function GetSideChainHeight

```text
rpc GetSideChainHeight (aelf.SInt32Value) returns (aelf.SInt64Value)
{
        option (aelf.is_view) = true;
}
```

Get current height of the specified side chain.

**Parameters:**

* **SInt32Value** - id of side chain

**Returns:**

Current height of the side chain.

### function GetParentChainHeight

```text
rpc GetParentChainHeight (google.protobuf.Empty) returns (aelf.SInt64Value)
{
        option (aelf.is_view) = true;
}
```

Get recorded height of parent chain

**Parameters:**

* **google.protobuf.Empty**

**Returns:**

Height of parent chain.

### function GetParentChainId

```text
rpc GetParentChainId (google.protobuf.Empty) returns (aelf.SInt32Value)
{
        option (aelf.is_view) = true;
}
```

Get id of the parent chain which can't be zero

**Parameters:**

* **google.protobuf.Empty**

**Returns:**

Parent chain id.

### function LockedBalance

```text
rpc GetSideChainBalance (aelf.SInt32Value) returns (aelf.SInt64Value) 
{
        option (aelf.is_view) = true;
}
```

Get locked token balance for side chain.

**Parameters:**

* **SInt32Value** - id of side chain

**Returns:**

Balance of the side chain

### function GetSideChainIdAndHeight

```text
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

* **google.protobuf.Empty**

**Returns:**

_**SideChainIdAndHeightDict**_ : A map contains id and height of side chains

### function GetSideChainIndexingInformationList

```text
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

* **google.protobuf.Empty**

**Returns:**

_**SideChainIndexingInformationList**_ : A list contains indexing information of side chains

### function GetAllChainsIdAndHeight

```text
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

* **Empty** - empty input

**Returns:**

_**SideChainIdAndHeightDict**_ : A map contains id and height of all chains

### function GetIndexedCrossChainBlockDataByHeight

```text
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

* **SInt64Value** - block height

**Returns:**

_**CrossChainBlockData**_

* **side\_chain\_block\_data** - cross chain block data of side chain
* **parent\_chain\_block\_data** - cross chain block data of parent chain

### function GetIndexedSideChainBlockDataByHeight

```text
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

* **SInt64Value** - height of side chain

**Returns:**

_**IndexedSideChainBlockData**_

* **side\_chain\_block\_data** - block data of side chain

### function GetBoundParentChainHeightAndMerklePathByHeight

```text
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

* **SInt64Value** - height of side chain

**Returns:**

_**CrossChainMerkleProofContext**_

* **bound\_parent\_chain\_height** - height of parent chain bound up with side chain
* **merkle\_path\_from\_parent\_chain** - merkle path generated from parent chain

### function GetChainInitializationData

```text
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

* **Int32Value** - id of side chain

**Returns:**

_**ChainInitializationData**_

* **chain\_id** - id of side chain
* **creator** - side chain creator
* **creation\_timestamp** - timestamp for side chain creation
* **extra\_information** - extra infomation like consensus and nativeToken etc.
* **creation\_height\_on\_parent\_chain** - height of side chain creation on parent chain
* **chain\_creator\_privilege\_preserved** - creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
* **side\_chain\_token\_symbol** - token symbol of side chain

