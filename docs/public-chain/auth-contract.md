## Authority contracts

AElf authority is centered around three central concepts: **Association**, **Referendum** and **Parliament**. These each have an associated smart contract within AElf. They are all centered around the concept of **Organizations** and **Proposals**. This section explains the difference between the three contracts.

### ACS3 and proposals

These contracts all implement proposal functionality that is defined in AElf's ACS3 standard:

```Protobuf
service AuthorizationContract {
    rpc CreateProposal (CreateProposalInput) returns (aelf.Hash) { }
    rpc Approve (ApproveInput) returns (google.protobuf.BoolValue) { }
    rpc Reject (aelf.Hash) returns (google.protobuf.Empty) { }
    rpc Abstain (aelf.Hash) returns (google.protobuf.Empty) { }
    rpc Release (aelf.Hash) returns (google.protobuf.Empty) { }
}

message CreateProposalInput {
    string contract_method_name = 1;
    aelf.Address to_address = 2;
    bytes params = 3;
    google.protobuf.Timestamp expired_time = 4;
    aelf.Address organization_address = 5;
    string proposal_description_url = 6;
    aelf.Hash token = 7;
}
```

The mechanics of proposal **creations** and proposal **approval** are similar in the three contracts but with small differences that will be explained later in this section. Essentially a proposal is created within an **Organization** and will be either approved or rejected based on the organizations thresholds. When creating a proposal, a log with its ID will be placed in the transaction result.
You can calculate the **proposal id** since the id generation method is public and definitive. It will generate id with the **token** you provide. And with **proposal_description_url**, you can provide external url for proposal description.

When approving a **proposal** the users (address') send **approval(s)** to the contract by calling the **Approve** method. The contracts usually aggregate these approvals until reaching a certain threshold. When the required amount of approvals is reached, the proposal can then be released. The release usually triggers an inline transaction to another contract and transaction log.

It's also possible to actively vote against the proposal by calling **Reject** or **Abstain** to vote blanc.

### Association, Referendum and Parliament

As stated before AElf's Authority contracts implement **ACS3** and are centered around proposals and organizations. That said they differ in some important ways and this section explains the differences between the contracts. 

Before talking about the difference it's useful to introduce some common characteristics between the different Organizations and Proposals in AElf's authority contracts: 
- Organizations common properties:
  - a **hash** and **address** used to identify the organization.
  - a **release threshold** as the specific threshold for proposal releasing.
- Proposals common properties:
  - an **Hash** (ID) to identify the proposal.
  - a **proposer** that is the sender of the transaction.
  - the **address** of the organization.
  - an **expiration** time.
  - a target **contract address**, **method name** and **parameters** that will be used when the proposal is finally released.

The main difference between the **proposals** is how and by who they can be approved.

#### Association

```Protobuf
service AssociationContract {
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc ChangeOrganizationMember(OrganizationMemberList) returns (google.protobuf.Empty){}
    rpc GetOrganization (aelf.Address) returns (Organization) { }
    rpc CalculateOrganizationAddress(CreateOrganizationInput) returns (aelf.Address){}
}

message Organization{
    OrganizationMemberList organization_member_list = 1;
    acs3.ProposalReleaseThreshold proposal_release_threshold = 2;
    acs3.ProposerWhiteList proposer_white_list = 3;
    aelf.Address organization_address = 4;
    aelf.Hash organization_hash = 5;
}

message OrganizationMemberList {
    repeated aelf.Address organization_members = 1;
}

message ProposalInfo {
    // ...
    repeated aelf.Address approvals = 8;
    repeated aelf.Address rejections = 9;
    repeated aelf.Address abstentions = 10;
}
```

In **Association** (implemented by AssociationAuthContract) **Organizations** have **members**. Any address in **proposer_white_list** has the permission to propose. Only members of the **Organization** can review its proposal and each reviewer can only review a proposal once. Once the proposal reached the Organizations' threshold only the Proposer can release it.

Requirements for a proposal to be *releasable*:
  
    COUNT(approval) >= THRESHOLD(approval) &&  COUNT(rejection) < THRESHOLD(rejection) && COUNT(abstention) < THRESHOLD(abstention) && SUM(COUNT(approval), COUNT(rejection), COUNT(abstention)) >= THRESHOLD(SUM)

#### Referendum

```Protobuf
service ReferendumContract {
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc ReclaimVoteToken (aelf.Hash) returns (google.protobuf.Empty) { }
    rpc GetOrganization (aelf.Address) returns (Organization) { }
    rpc CalculateOrganizationAddress(CreateOrganizationInput) returns (aelf.Address){}
}

message Organization {
    acs3.ProposalReleaseThreshold proposal_release_threshold = 1;
    string token_symbol = 2;
    aelf.Address organization_address = 3;
    aelf.Hash organization_hash = 4;
    acs3.ProposerWhiteList proposer_white_list = 5;
}
```

The **referendum** contract is essentially for **voting** by **locking** tokens (which token is defined by the **Organization**). Any address in **proposer_white_list** has the permission to propose. And the token contract is called to lock a certain amount of tokens during **Approve**/**Reject**/**Abstain** automatically. The amount of tokens locked is determined by existing allowance to the Referendum contract. Tokens can after be reclaimed when the transaction is released or expired.

Requirements for a proposal to be *releasable*:
  
    COUNT(approval_token) >= THRESHOLD(approval_token) &&  COUNT(rejection_token) < THRESHOLD(rejection_token) && COUNT(abstention_token) < THRESHOLD(abstention_token) && SUM(COUNT(approval_token), COUNT(rejection_token), COUNT(abstention_token)) >= THRESHOLD(locked_token)

#### Parliament

```Protobuf
service ParliamentContract {
    rpc Initialize(InitializeInput) returns (google.protobuf.Empty) { }
    rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }
    rpc GetOrganization (aelf.Address) returns (Organization) { }
    rpc GetGenesisOwnerAddress (google.protobuf.Empty) returns (aelf.Address) { }
}

message Organization {
    bool proposer_authority_required = 1;
    aelf.Address organization_address = 2;
    aelf.Hash organization_hash = 3;
    acs3.ProposalReleaseThreshold proposal_release_threshold = 4;
    bool parliament_member_proposing_allowed = 5;
}

message ProposalInfo {
    // ...
    repeated aelf.Address approved_representatives = 8;
}
```

The **Parliament** has the same behavior as the Association except that the members of parliament organization are current miner list but not arbitrarily ones. All parliament organizations share the same **white_list**, which can be update by governing proposal.  
Any address in **proposer_white_list** has the permission to propose. Any member of one parliament organization is able to propose if **parliament_member_proposing_allowed** is true.

Requirements for a proposal to be *releasable*:
  
    COUNT(approval) / COUNT (MINER_LIST) >= THRESHOLD(approval) &&  COUNT(rejection) / COUNT (MINER_LIST) < THRESHOLD(rejection) && COUNT(abstention) / COUNT (MINER_LIST) < THRESHOLD(abstention) && SUM(COUNT(approval), COUNT(rejection), COUNT(abstention)) / COUNT (MINER_LIST) >= THRESHOLD(SUM)


