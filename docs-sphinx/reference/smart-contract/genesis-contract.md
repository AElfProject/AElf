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
