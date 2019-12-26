# Genesis Contract

## Functions

### Detailed Description

Defines C# API functions for genesis contract.

## Functions Documentation

### Views

#### function GetDeployedContractAddressList

```protobuf
rpc GetDeployedContractAddressList (google.protobuf.Empty) returns (AddressList) 
{
		option (aelf.is_view) = true;
}
message AddressList {
    repeated aelf.Address value = 1;
}
```

Get the list contains address of deployed contracts.

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Address list of the deployed contracts.

#### function CurrentContractSerialNumber

```protobuf
rpc CurrentContractSerialNumber (google.protobuf.Empty) returns (google.protobuf.UInt64Value) 
{
		option (aelf.is_view) = true;
}
```

Ge serial number of genesis contract.

**Parameters:**

- **google.protobuf.Empty**

**Returns:**

Serial number of genesis contract.

#### function GetContractInfo

```protobuf
rpc GetContractInfo (aelf.Address) returns (ContractInfo) 
{
		option (aelf.is_view) = true;
}
message ContractInfo {
    uint64 serial_number = 1;
    aelf.Address author = 2;
    int32 category = 3;
    aelf.Hash code_hash = 4;
    bool is_system_contract = 5;
}
```

Get detaiedl infomation of the specified contract.

**Parameters:**

- **Address** - address of specified contract

**Returns:**

Detailed infomation of the specified contract.

#### function GetContractAuthor

```protobuf
rpc GetContractAuthor (aelf.Address) returns (aelf.Address)
{
		option (aelf.is_view) = true;
}
```

Get author of the specified contract.

**Parameters:**

- **Address** - address of specified contract

**Returns:**

Author of the specified contract.

#### function GetContractHash

```protobuf
rpc GetContractHash (aelf.Address) returns (aelf.Hash) 
{
		option (aelf.is_view) = true;
}
```

Get code hash of the specified contract.

**Parameters:**

- **Address** - address of a contract

**Returns:**

code hash of the specified contract.

#### function GetContractAddressByName

```protobuf
rpc GetContractAddressByName (aelf.Hash) returns (aelf.Address)
{
		option (aelf.is_view) = true;
}
```

Get address of a contract by it's name. 

**Parameters:**

- **Hash** - name hash of a contract

**Returns:**

Address of the specified contract

#### function GetSmartContractRegistrationByAddress

```protobuf
rpc GetSmartContractRegistrationByAddress (aelf.Address) returns (aelf.SmartContractRegistration) 
{
		option (aelf.is_view) = true;
}

message SmartContractRegistration {
    int32 category = 1;
    bytes code = 2;
    Hash code_hash = 3;
}
```

Get registration of a smart contract by it's address.

**Parameters:**

- **Address** - address of a smart contract

**Returns:**

Registration of the smart contract

#### function ValidateSystemContractAddress

```protobuf
rpc ValidateSystemContractAddress(ValidateSystemContractAddressInput) returns (google.protobuf.Empty)
{
		option (aelf.is_view) = true;
}

message ValidateSystemContractAddressInput {
    aelf.Hash system_contract_hash_name = 1;
    aelf.Address address = 2;
}
```

Validate whether the input system contract is legal.

**Parameters:**

***ValidateSystemContractAddressInput*** 

- **Hash** - name hash of a contract
- **Address** - address of a contract

**Returns:**

**google.protobuf.Empty**

### Actions

#### function DeploySystemSmartContract

```protobuf
rpc DeploySystemSmartContract (SystemContractDeploymentInput) returns (aelf.Address) {}

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

Deploy a system smart contract on chain.

**Parameters:**

***SystemContractDeploymentInput*** 

- ***category*** - contract type 
- ***code*** - byte array of system contract code
- ***name*** - name hash of system contract
- ***transaction_method_call_list*** - list of methods called by system transaction

**Returns:**

Address of the deployed contract.

### function ProposeNewContract

```protobuf
rpc ProposeNewContract (ContractDeploymentInput) returns (aelf.Hash) {}

message ContractDeploymentInput {
    sint32 category = 1;
    bytes code = 2;
}
```

Create a proposal for deployment of new contract.

**Parameters:**

***ContractDeploymentInput*** 

- **category** - contract type
- **code** - byte array of new contract codes

**Returns:**

Hash of contract-deployment-input.

### function ProposeUpdateContract

```protobuf
rpc ProposeUpdateContract (ContractUpdateInput) returns (aelf.Hash) {}

message ContractUpdateInput {
    aelf.Address address = 1;
    bytes code = 2;
}
```

Create a proposal to update specified contract.

**Parameters:**

***ContractUpdateInput*** 

- **address** - address of the contract to be updated
- **code** - byte array of new contract codes

**Returns:**

Hash of contract-update-input.

### function ProposeContractCodeCheck

```protobuf
rpc ProposeContractCodeCheck (ContractCodeCheckInput) returns (aelf.Hash) {}

message ContractCodeCheckInput{
    bytes contract_input = 1;
    bool is_contract_deployment = 2;
}
```

Create a proposal to check codes of input contract.

**Parameters:**

***ContractCodeCheckInput***

- **contract_input** - byte array of the contract codes to be checked

- **is_contract_deployment** - whether input contract to be deployed or updated

**Returns:**

Hash of contract-code-check-input.

### function ReleaseApprovedContract

```protobuf
rpc ReleaseApprovedContract (ReleaseContractInput) returns (google.protobuf.Empty) {}

message ReleaseContractInput {
    aelf.Hash proposal_id = 1;
    aelf.Hash proposed_contract_input_hash = 2;
}
```

Release a contract proposal which has been approved.

**Parameters:**

***ReleaseContractInput***

- **proposal_id** - hash of a proposal

- **proposed_contract_input_hash** - input hash of the proposed contract

### function ReleaseCodeCheckedContract

```protobuf
rpc ReleaseCodeCheckedContract (ReleaseContractInput) returns (google.protobuf.Empty) {}

message ReleaseContractInput {
    aelf.Hash proposal_id = 1;
    aelf.Hash proposed_contract_input_hash = 2;
}
```

Release the proposal which has passed code-check.

**Parameters:**

***ReleaseContractInput*** 

- **proposal_id** - hash of a proposal
- **proposed_contract_input_hash** - input hash of the proposed contract

### function DeploySmartContract

```protobuf
rpc DeploySmartContract (ContractDeploymentInput) returns (aelf.Address) {}

message ContractDeploymentInput {
    sint32 category = 1;
    bytes code = 2;
}
```

Deploy a smart contract on chain.

**Parameters:**

***ContractDeploymentInput*** 

- **category** - contract type
- **code** - byte array of the contract codes

**Returns:**

Address of the deployed smart contract.

### function UpdateSmartContract

```protobuf
rpc UpdateSmartContract (ContractUpdateInput) returns (aelf.Address) {}

message ContractUpdateInput {
    aelf.Address address = 1;
    bytes code = 2;
}
```

Update a smart contract on chain.

**Parameters:**

***ContractUpdateInput***

- **address** - address of the smart contract to be updated
- **code** - byte array of new contract codes

**Returns:**

Address of the updated smart contract.

### function Initialize

```protobuf
rpc Initialize (InitializeInput) returns (google.protobuf.Empty) {}

message InitializeInput{
    bool contract_deployment_authority_required = 1;
}
```

Initialize the zero contract.

**Parameters:**

***InitializeInput*** 

- **contract_deployment_authority_required** - whether contract deployment requires authority

### function ChangeGenesisOwner

```protobuf
rpc ChangeGenesisOwner (aelf.Address) returns (google.protobuf.Empty) {}
```

Change owner of the genesis contract.

**Parameters:**

- **Address** - address of new owner

### function SetContractProposerRequiredState

```protobuf
rpc SetContractProposerRequiredState (google.protobuf.BoolValue) returns (google.protobuf.Empty) {}
```

Set authority of contract deployment.

**Parameters:**

- **google.protobuf.BoolValue ** - whether contract deployment requires authority
