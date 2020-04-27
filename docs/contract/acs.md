# AElf Contract Standard (ACS)

Think about **Interface Segregation Principle**. In AElf, a smart contract can choose to inherit from one or more **ACSs**. To implement an ACS, developers need to specify ACSs as bases of the contract.The code snippet below shows how to do this:

```Proto

import "acs1.proto";

service EconomicContract {

    option (aelf.base) = "acs1.proto";

    // Actions, views...

}
```

When doing this you can `override` (implement) the ACS's methods in your contract implementation in C#.

## ACS0 - Genesis

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs0.proto)

**Description:** For Contract Zero to deploy, update and maintain other smart contracts. This is implemented by the Genesis contract (BasicContractZero) in AElf.

``` Protobuf
    rpc DeploySmartContract (ContractDeploymentInput) returns (aelf.Address) { }
    rpc UpdateSmartContract (ContractUpdateInput) returns (aelf.Address) { }
    rpc ChangeContractAuthor (ChangeContractAuthorInput) returns (google.protobuf.Empty { }
    ...
```

As you can see, it is mainly used for managing smart contracts.

## ACS1 - Fee information

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs1.proto)

**Description:** For smart contracts to set and provide method fee information. Most of the genesis contracts in AElf take this as a base.

```Protobuf
    rpc SetMethodFee (MethodFees) returns (google.protobuf.Empty) { }
    rpc GetMethodFee (google.protobuf.StringValue) returns (MethodFees) { }
```

This ACS is essential for the economic system as it defines the chargeable method. The `GetMethodFee` method will take effects before executing related transactions.

## ACS2 - Parallel resource information

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs2.proto)

**Description:**
Smart contracts can use this interface to inform the parallel executing mechanism resources usage of specific methods in this contract.

```Protobuf
    rpc GetResourceInfo (aelf.Transaction) returns (ResourceInfo) { }
```

## ACS3 - Proposal and approval

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs3.proto)

**Description:** This ACS defines proposal and approval functionalities. With ACS3, a contract can design specific multi-sign mechanisms.


``` Protobuf
    rpc CreateProposal (CreateProposalInput) returns (aelf.Hash) { }
    rpc Approve (ApproveInput) returns (google.protobuf.BoolValue) { }
    rpc Release(aelf.Hash) returns (google.protobuf.Empty){ }
    rpc GetProposal(aelf.Hash) returns (ProposalOutput) { }
```

As you can see in the code snippet above, the standard defines creation, approval, and release functionality. In AElf three contracts implement this functionality, the referendum, association, and parliament contract. 


## ACS4 - Consensus

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs4.proto)

**Description:** For anyone who wants to customize a new blockchain to implement a new consensus.

```Protobuf
    rpc GetConsensusCommand (google.protobuf.BytesValue) returns (ConsensusCommand) { }
    rpc GetConsensusExtraData (google.protobuf.BytesValue) returns (google.protobuf.BytesValue) { }
    rpc GenerateConsensusTransactions (google.protobuf.BytesValue) returns (TransactionList) { }
    rpc ValidateConsensusBeforeExecution (google.protobuf.BytesValue) returns (ValidationResult) { }
    rpc ValidateConsensusAfterExecution (google.protobuf.BytesValue) returns (ValidationResult) { }
```

For now, only AElf's AEDPoSContract implements this interface. If one system contract deployed with its contract address can be bind to `ConsensusContractSystemName` (by Contract Zero), then the consensus process of the current blockchain will use the logic provided by this system contract.

## ACS5 - Method calling threshold

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs5.proto)

**Description:** For one contract to check the threshold for others of calling himself, either the sender's balance or sender's allowance to this contract.

```Protobuf
    rpc SetMethodCallingThreshold (SetMethodCallingThresholdInput) returns (google.protobuf.Empty) { }
    rpc GetMethodCallingThreshold (google.protobuf.StringValue) returns (MethodCallingThreshold) { }
```

## ACS6 - Random number generation

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs6.proto)

**Description:** ACS6 is aiming at providing two standard interfaces for requesting and getting random numbers. It implies using commit-reveal schema during random number generation and verification, though other solutions can also expose their services via these two interfaces. For now, only AEDPoSContract implements this in AElf.

```Protobuf
    rpc RequestRandomNumber (google.protobuf.Empty) returns (RandomNumberOrder) { }
    rpc GetRandomNumber (aelf.Hash) returns (aelf.Hash) { }
```

In the **commit-reveal schema**, the user needs to call `RequestRandomNumberInput` on his initiative as well as provide a block height number as the minimum height to enable the query of random number. Then the contract implemented ACS6 needs to returns:
1) a hash value as the token for querying random numbers.
2) a negotiated block height to enable related queries. When the chain reaches the block height, the user can use that token, which is a hash value, call `GetRandomNumber` to query the random number.

The implementation of ACS6 in the `AEDPoS Contract` shows how **commit-reveal schema** works.

## ACS7 - Cross-chain

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs7.proto)

**Description:**
ACS7 defines methods for cross chain contract to provide cross chain data and indexing functionalities, currently used by AElfs CrossChainContract.

```Protobuf
    rpc RecordCrossChainData (CrossChainBlockData) returns (google.protobuf.Empty) { }
    rpc CreateSideChain (SideChainCreationRequest) returns (aelf.SInt32Value) { }
    rpc Recharge (RechargeInput) returns (google.protobuf.Empty) { }
    rpc DisposeSideChain (aelf.SInt32Value) returns (aelf.SInt64Value) { }
```

The cross-chain ACS is mainly used for managing side-chains.

## ACS8 - Contract fee

[standard](https://github.com/AElfProject/AElf/blob/dev/protobuf/acs8.proto)

**Description:**
If one contract chooses to inherit from ACS8, the execution of every transaction of this contract will consume resource tokens of the current contract.

``` Protobuf
    rpc BuyResourceToken (BuyResourceTokenInput) returns (google.protobuf.Empty) { }
```