# AElf Contract Standard (ACS)

Description

Think about **Interface Segregation Principle**. In AElf, one smart contract can choose to inherit from one or more *ACS*s. For each ACS, just add two lines of code.

```Proto

import "acs1.proto";

...

// service
    option (aelf.base) = "acs1.proto";

```
Thus you can `override` the methods defined in corresponding proto file in your contract implementation in C#.

## ACS0

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs0.proto)

Description

For Contract Zero to deploy, update and maintain other smart contracts.

## ACS1

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs1.proto)

Description

For smart contracts to set and provide method fee information.

The `GetMethodFee` method will take effects before executing related transactions.

## ACS2

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs2.proto)

Description

For smart contracts to set and provide resource information to support parallel executing.

## ACS3

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs3.proto)

Description

For smart contract to implement proposal and approval functionalities. With ACS3, contract can design specific multi-sign mechanism.

## ACS4

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs4.proto)

Description

For anyone who wants to customize a new blockchain to implement a new consensus.

If one system contract deployed with it's contract address can be bind to `ConsensusContractSystemName` (by Contract Zero), then the consensus process of current blockchain will use the logic provided by this system contract.

## ACS5

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs5.proto)

Description

For one contract to check the threshold for others of calling himself, either the sender's balance or sender's allowance to this contract.

## ACS6

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs6.proto)

Description

ACS6 is aiming at providing two standard interfaces for requesting and getting random numbers. It implies using commit-reveal schema during random number generation and verification, though other solutions can also expose their services via these two interfaces.
In the commit-reveal schema, the user needs to call `RequestRandomNumberInput` on his initiative as well as provide a block height number as the minimum height to enable the query of random number. Then the contract implemented ACS6 needs to return 1) a hash value as the token for querying random number and 2) a negotiated block height to enable related query. When the block height reaches, the user can use that token, which is a hash value, call `GetRandomNumber` to query the random number.
The implementation of ACS6 in the `AEDPoS Contract` shows how commit-reveal schema works.

## ACS7

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs7.proto)

Description

ACS7 defines methods for cross chain contract to provide cross chain data and indexing functionalities.

## ACS8

[standard code link](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs8.proto)

Description

If one contract choose to inherit from ACS8, the execution of every transaction of this contract will consume resource tokens of current contract.