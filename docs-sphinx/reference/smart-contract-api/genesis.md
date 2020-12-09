# Genesis Contract

This page describes available methods on the Genesis Contract.

## **Actions**

### DeploySystemSmartContract

```protobuf
rpc DeploySystemSmartContract (SystemContractDeploymentInput) returns (aelf.Address){}

message SystemContractDeploymentInput
{
    message SystemTransactionMethodCall
    {
        string method_name = 1;
        bytes params = 2;
    }
    message SystemTransactionMethodCallList
    {
        repeated SystemTransactionMethodCall value = 1;
    }
    sint32 category = 1;
    bytes code = 2;
    aelf.Hash name = 3;
    SystemTransactionMethodCallList transaction_method_call_list = 4;
}
```

Deploys a system smart contract on chain.

- **SystemContractDeploymentInput**
  - **category**: contract type.
  - **code**: byte array of system contract code.
  - **name**: name hash of system contract.
  - **transaction_method_call_list**: list of methods called by system transaction.

- **Returns**
  - **value**: address of the deployed contract.

### ProposeNewContract

```protobuf
rpc ProposeNewContract (ContractDeploymentInput) returns (aelf.Hash){}

message ContractDeploymentInput {
    sint32 category = 1;
    bytes code = 2;
}
```

Propose new contract deployment.

- **ContractDeploymentInput**
  - **category**: contract type (usually 0 for now)
  - **code**: byte array that represents the contract code

- **Returns**
  - **value:**: Hash of the **ContractDeploymentInput** object.

### ProposeUpdateContract

```protobuf
rpc ProposeUpdateContract (ContractUpdateInput) returns (aelf.Hash){}

message ContractUpdateInput {
    aelf.Address address = 1;
    bytes code = 2;
}
```

Creates a proposal to update the specified contract.

- **ContractUpdateInput**
  - **address**: address of the contract to be updated
  - **code**: byte array of the contract's new code

- **Returns**
  - **value**: Hash of the **ContractUpdateInput** object.

### ProposeContractCodeCheck

```protobuf
rpc ProposeContractCodeCheck (ContractCodeCheckInput) returns (aelf.Hash) {}

message ContractCodeCheckInput{
    bytes contract_input = 1;
    bool is_contract_deployment = 2;
    string code_check_release_method = 3;
    aelf.Hash proposed_contract_input_hash = 4;
    sint32 category = 5;
}
```

Propose to check the code of a contract.

- **ContractCodeCheckInput**
  - **contract_input**: byte array of the contract code to be checked
  - **is_contract_deployment**: whether the input contract is to be deployed or updated
  - **code_check_release_method**: method to call after code check complete (DeploySmartContract or UpdateSmartContract)
  - **proposed_contract_input_hash**: id of the proposed contract
  - **category**: contract category (always 0 for now)

- **Returns**
  - **value**: Hash of the proposed contract.

### ReleaseApprovedContract

```protobuf
rpc ReleaseApprovedContract (ReleaseContractInput) returns (google.protobuf.Empty){}

message ReleaseContractInput {
    aelf.Hash proposal_id = 1;
    aelf.Hash proposed_contract_input_hash = 2;
}
```

Releases a contract proposal which has been approved.

- **ReleaseContractInput**
  - **proposal_id**: hash of the proposal.
  - **proposed_contract_input_hash**: id of the proposed contract

### ReleaseCodeCheckedContract

```protobuf
rpc ReleaseCodeCheckedContract (ReleaseContractInput) returns (google.protobuf.Empty){}

message ReleaseContractInput {
    aelf.Hash proposal_id = 1;
    aelf.Hash proposed_contract_input_hash = 2;
}
```

Release the proposal which has passed the code check.

- **ReleaseContractInput**
  - **proposal_id**: hash of the proposal
  - **proposed_contract_input_hash**: id of the proposed contract

### DeploySmartContract

```protobuf
rpc DeploySmartContract (ContractDeploymentInput) returns (aelf.Address){}

message ContractDeploymentInput {
    sint32 category = 1;
    bytes code = 2;
}
```

Deploys a smart contract on chain.

- **ContractDeploymentInput**
  - **category**: contract type (usually 0)
  - **code**: byte array of the contract code

- **Returns**
  - **value**: address of the deployed smart contract.

### UpdateSmartContract

```protobuf
rpc UpdateSmartContract (ContractUpdateInput) returns (aelf.Address){}

message ContractUpdateInput {
    aelf.Address address = 1;
    bytes code = 2;
}
```

Updates a smart contract on chain.

- **ContractUpdateInput**
  - **address**: address of the smart contract to be updated
  - **code**: byte array of the updated contract code

- **Returns**
  - **value**: address of the updated smart contract.

### Initialize

```protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty){}

message InitializeInput{
    bool contract_deployment_authority_required = 1;
}
```

Initializes the genesis contract.

- **InitializeInput**
  - **contract_deployment_authority_required**: whether contract deployment/update requires authority.

### ChangeGenesisOwner

```protobuf
rpc ChangeGenesisOwner (aelf.Address) returns (google.protobuf.Empty){}
```

Change the owner of the genesis contract.

- **Address**: address of new genesis owner

### SetContractProposerRequiredState

```protobuf
rpc SetContractProposerRequiredState (google.protobuf.BoolValue) returns (google.protobuf.Empty){}
```

Set authority of contract deployment.

- **google.protobuf.BoolValue**: whether contract deployment/update requires contract proposer authority

### ChangeContractDeploymentController

```protobuf
rpc ChangeContractDeploymentController (acs1.AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Modify the contract deployment controller authority. Note: Only old controller has permission to do this.

- **AuthorityInfo**: new controller authority info containing organization address and contract address that the organization belongings to

### ChangeCodeCheckController

```protobuf
rpc ChangeCodeCheckController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Modifies the contract code check controller authority. Note: Only old controller has permission to do this.

- **AuthorityInfo**: new controller authority info containing organization address and contract address that the organization belongings to

### SetInitialControllerAddress

```protobuf
rpc SetInitialControllerAddress (aelf.Address) returns (google.protobuf.Empty) {}
```

Sets initial controller address for **CodeCheckController** and **ContractDeploymentController**

- **Address**: initial controller (which should be parliament organization as default)

### GetContractDeploymentController

```protobuf
rpc GetContractDeploymentController (google.protobuf.Empty) returns (acs1.AuthorityInfo) {
        option (aelf.is_view) = true;
}
message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Returns **ContractDeploymentController** authority info.

- **Returns**: **ContractDeploymentController** authority info.

### GetCodeCheckController

```protobuf
rpc GetCodeCheckController (google.protobuf.Empty) returns (acs1.AuthorityInfo) {
        option (aelf.is_view) = true;
}
message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Returns **CodeCheckController** authority info.

- **Returns**: **CodeCheckController** authority info.

## **Views methods**

### CurrentContractSerialNumber

```protobuf
rpc CurrentContractSerialNumber (google.protobuf.Empty) returns (google.protobuf.UInt64Value){}
```

Gets the current serial number of genesis contract (corresponds to the serial number that will be given to the next deployed contract).

- **Returns**
  - **value**: serial number of the genesis contract.

### GetContractInfo

```protobuf
rpc GetContractInfo (aelf.Address) returns (ContractInfo){}

message ContractInfo {
    uint64 serial_number = 1;
    aelf.Address author = 2;
    int32 category = 3;
    aelf.Hash code_hash = 4;
    bool is_system_contract = 5;
    int32 version = 6;
}
```

Gets detailed information about the specified contract.

- **Address**: address the contract

**Returns**
A **ContractInfo** object that represents detailed information about the specified contract.

### GetContractAuthor

```protobuf
rpc GetContractAuthor (aelf.Address) returns (aelf.Address){}
```

Get author of the specified contract.

- **Address**: address of specified contract

- **Returns**
  - **value**: author of the specified contract.

### GetContractHash

```protobuf
rpc GetContractHash (aelf.Address) returns (aelf.Hash){}
```

Gets the code hash of the contract at the specified address.

- **Address**: address of a contract

- **Returns**
  - **value**: the code hash of the contract.

### GetContractAddressByName

```protobuf
rpc GetContractAddressByName (aelf.Hash) returns (aelf.Address){}
```

Gets the address of a system contract by its name.

- **Hash**: name hash of the contract

- **Returns**
  - **value**: Address of the specified contract.

### GetSmartContractRegistrationByAddress

```protobuf
rpc GetSmartContractRegistrationByAddress (aelf.Address) returns (aelf.SmartContractRegistration){}

message SmartContractRegistration {
    int32 category = 1;
    bytes code = 2;
    Hash code_hash = 3;
    bool is_system_contract = 4;
    int32 version = 5;
}
```

Gets the registration of a smart contract by its address.

- **Address** - address of a smart contract

- **Returns**
  - **value**: Registration object of the smart contract.

### GetSmartContractRegistrationByCodeHash

```protobuf
rpc GetSmartContractRegistrationByCodeHash (aelf.Hash) returns (aelf.SmartContractRegistration){}

message SmartContractRegistration {
    int32 category = 1;
    bytes code = 2;
    Hash code_hash = 3;
    bool is_system_contract = 4;
    int32 version = 5;
}
```

Gets the registration of a smart contract by code hash.

- **Hash** - contract code hash

- **Returns**
  - **value**: registration object of the smart contract.

### ValidateSystemContractAddress

```protobuf
rpc ValidateSystemContractAddress(ValidateSystemContractAddressInput) returns (google.protobuf.Empty){}

message ValidateSystemContractAddressInput {
    aelf.Hash system_contract_hash_name = 1;
    aelf.Address address = 2;
}
```

Validates whether the input system contract exists.

- **ValidateSystemContractAddressInput**
  - **Hash** - name hash of the contract
  - **Address** - address of the contract
