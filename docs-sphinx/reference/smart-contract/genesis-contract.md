# Overview

Genesis Contract, also known as the Zero Contract, is mainly used to deploy and maintain smart contracts running on the aelf blockchain.

# Deploy and update contracts

There is a critical data structure defined in `aelf/core.proto`, called SmartContractRegistration:

```C#
message SmartContractRegistration {
    // The category of contract code(0: C#).
    sint32 category = 1;
    // The byte array of the contract code.
    bytes code = 2;
    // The hash of the contract code.
    Hash code_hash = 3;
    // Whether it is a system contract.
    bool is_system_contract = 4;
    // The version of the current contract.
    int32 version = 5;
    // The version of the contract.
    string contract_version = 6;
    // The address of the current contract.
    Address contract_address = 7;
    // Indicates if the contract is the user contract.
    bool is_user_contract = 8;
}
```
Smart Contract code is store in the `code` field.

However, each `SmartContractRegistration` entity is not a one-to-one correspondence with the contract, and its storage structure is:

```C#
public MappedState<Hash, SmartContractRegistration> SmartContractRegistrations { get; set; }
```

`SmartContractRegistration` entity can be fetched by the hash value of the contract code. In fact, it is only written once when deploying the contract.

The data structure that corresponds one-to-one with contracts is called `ContractInfo`.
Structure `ContractInfo` is defined in `acs0.proto`.

```C#
message ContractInfo
{
    // The serial number of the contract.
    int64 serial_number = 1;
    // The author of the contract, this is the person who deployed the contract.
    aelf.Address author = 2;
    // The category of contract code(0: C#).
    sint32 category = 3;
    // The hash of the contract code.
    aelf.Hash code_hash = 4;
    // Whether it is a system contract.
    bool is_system_contract = 5;
    // The version of the current contract.
    int32 version = 6;
    string contract_version = 7;
    // Indicates if the contract is the user contract.
    bool is_user_contract = 8;
}
```

We use the MappedState to store related instances.

```C#
public MappedState<Address, ContractInfo> ContractInfos { get; set; }
```

From the `code_hash` field of `ContractInfo`, it is not difficult to guess:

1. When trying to retrieve the contract code, the `code_hash` of ContractInfo is first read, and then the contract code itself is read from `State.SmartContractRegistrations` mapped state.
2. Upgrading a contract on aelf is actually replacing the `code_hash` of `ContractInfo`.

# Calculation of contract address

The contract address is actually calculated through a field that increases with the number of contract deployments.

```C#
public Int64State ContractSerialNumber { get; set; }
```

Its calculation process is located in the `DeploySmartContract` method:

```C#
var contractAddress = AddressHelper.BuildContractAddress(Context.ChainId, serialNumber);
```

- The contract address of each chain of aelf is different.
- The contract address is not related to the contract code, but only to the order in which it is deployed on this chain.
    - Therefore, when testing newly written contracts in `aelf-boilerplate` or `aelf-developer-tools`, the new contract always has a fixed address.

After the 1.6.0 version, Salt is added to the imported parameter of the deployment/upgrade contract. The contract address is calculated by using the Deployer address of the deployment account and the hash value Salt.

```C#
var contractAddress = AddressHelper.ComputeContractAddress(deployer, salt);
```

- Deploying contracts with the same account and using the same Salt can make the contract address of each chain of aelf the same.

# Contract deployment and update process

## Deploy contract with audit

The current pipeline starts with Propose, which generates a parliamentary proposal.
When more than 2/3 of the BPs agree to deploy/update, a new proposal is released to request code inspection. 
Finally, after the code inspection is passed, the real contract deployment/upgrade will be achieved through the proposal of releasing code inspection.

## Deploy contract without audit

Developers send deployment/update user contract transactions, generate a parliamentary CodeCheck proposal, and when more than 2/3 of the BPs conduct code checks and pass, achieve real contract deployment/upgrade through the proposal of automatically releasing code checks.

## Contract deployment and upgrade new version number

When upgrading a contract, check the contract version information
- If the contract version is less than or equal to the original contract version, the upgrade contract transaction fails
  The old version of the contract only has a version number after being upgraded.
- If the version number is increasing , the upgrade contract transaction is successful.

In the updateSmartContract method, increase the version number judgment:

```C#
var contractInfo = Context.UpdateSmartContract(contractAddress, reg, null, info.ContractVersion);
Assert(contractInfo.IsSubsequentVersion,
    $"The version to be deployed is lower than the effective version({info.ContractVersion}), please correct the version number.");
```

# Contract error message


| Method | Error message | Note |
| --- | --- | --- |
| DeployUserSmartContract | No permission. | Trying to deploy smart contract to an aelf private sidechain, and the transaction sender is not in allowlist. |
|  | contract code has already been deployed before. | Contract code deployed  |
|  | Already proposed. | Duplicate deployment request |
| UpdateUserSmartContract | Contract not found. | Contract does not exist |
|  | No permission. | The updated contract author is not myself |
|  | Code is not changed. | The contract code has not changed |
|  | contract code has already been deployed before. | Contract code deployed |
|  | The version to be deployed is lower than the effective version({currentVersion}), please correct the version number. | Updated contract version number is too low  |
|  | Already proposed. | Duplicate update request |
