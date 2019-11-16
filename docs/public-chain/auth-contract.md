## Authority contracts

AElf authority is centered around three central concepts: **Association**, **Referendum** and **Parliament**. These each have an associated smart contract within AElf. They are all centered around the concept of **Organizations** and **Proposals**. This section explains the difference between the three contracts.

### ACS3 and proposals

These contracts all implement proposal functionality that is defined in AElf's ACS3 standard:

```Protobuf
service AuthorizationContract {
    rpc CreateProposal (CreateProposalInput) returns (aelf.Hash) { }
    rpc Approve (ApproveInput) returns (google.protobuf.BoolValue) { }
    rpc Release(aelf.Hash) returns (google.protobuf.Empty){ }
}

message ApproveInput {
    aelf.Hash proposal_id = 1;
    sint64 quantity= 2;
}

message CreateProposalInput {
    string contract_method_name = 2;
    aelf.Address to_address = 3;
    bytes params = 4;
    google.protobuf.Timestamp expired_time = 5;
    aelf.Address organization_address = 6;
}
```

The mechanics of proposal **creations** and **proposal** approval are similar in the three contracts but with small differences that will be explained later in this section. Essentially a proposal is created within an **Organization** (defined by the implementations - strictly speaking you can implement ACS3 without the concept of an **Organization**). When created a log with the ID of the Proposal will be placed in the transaction result. Usually the ID of the proposal is the hash of creation **transaction ID** and the **CreateProposalInput**.

When approving a **proposal** the user (address) sends **approval(s)** to the contract. The contracts usually aggregate these approvals until reaching a certain threshold. When the required amount of approvals is reached, the proposal can then be released. The release usually triggers an inline transaction to another contract and transaction log.

### Association, Referendum and Parliament

As stated before AElf's Authority contracts implement **ACS3** and are centered around proposals and organizations. That said the they differ in some important ways and this section explains the differences between the contracts. 

Before talking about the difference it's useful to introduce some common characteristics between the different Organizations and Proposals in AElf's authority contracts: 
- Organizations common properties:
  - a **Hash** and **Address** used to identify the organization.
  - a **release threshold** which is the amount of approvals needed before **proposals** of this organization are *releasable*.
- Proposals common properties:
  - an **Hash** (ID) to identify the proposal.
  - a **proposer** that is the sender of the transaction.
  - the **address** of the organization.
  - an **expiration** time.
  - a target **contract address**, **method name** and **parameters** that will be used when the proposal is finally released.

The main difference between the **proposals** is how and by who they can be approved.

#### Association

```Protobuf
service AssociationAuthContract {
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc GetOrganization (aelf.Address) returns (Organization) { }
}

message Organization {
    // ...
    repeated Reviewer reviewers = 2;
}

message Reviewer {
    //...
    aelf.Address address  = 1;
    int32 weight = 2;
}

message ProposalInfo {
    // ...
    repeated aelf.Address approved_reviewer = 9;
}
```

In **Association** (implemented by AssociationAuthContract) **Organizations** have **reviewers** that each have an associated **Weight**. Only reviewers of the **Organization** can review its proposal and each reviewer can only review a proposal once. Once the proposal reached the Organizations' threshold only the Proposer can release it.

#### Referendum

```Protobuf
service ReferendumAuthContract {
    rpc Initialize (google.protobuf.Empty) return (google.protobuf.Empty) { }
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc ReclaimVoteToken (aelf.Hash) returns (google.protobuf.Empty) { }
    rpc GetOrganization (aelf.Address) returns (Organization) { }
}

message Organization {
    // ...
    string token_symbol = 2;
}
```

The **referendum** contract is essentially for **voting** by **locking** tokens (which token is defined by the **Organization**). Thus when approving, the token contract is called to lock a certain amount of tokens. The amount of tokens locked will be the amount specified in the **ApproveInput** quantity field. Tokens can after be reclaimed when the transaction is released or expired. This contract will also only allow one vote per proposal.

#### Parliament

```Protobuf
service ParliamentAuthContract {
    rpc Initialize(InitializeInput) returns (google.protobuf.Empty) { }
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc GetOrganization (aelf.Address) returns (Organization) { }
    rpc GetGenesisOwnerAddress (google.protobuf.Empty) returns (aelf.Address) { }
}

message Organization {
    // ...
    bool proposer_authority_required = 4;
    repeated aelf.Address proposer_white_list = 5;
}

message ProposalInfo {
    // ...
    repeated aelf.Address approved_representatives = 8;
}
```

The **Parliament** has the same behavior as the Association, but instead of having Reviewers approval is done by current producers or whitelisted addresses.


