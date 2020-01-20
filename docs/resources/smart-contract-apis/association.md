# Association Contract

## **CreateOrganization**

```Protobuf
rpc CreateOrganization (CreateOrganizationInput) returns (aelf.Address) { }

message CreateOrganizationInput {
    string token_symbol = 1;
    acs3.ProposalReleaseThreshold proposal_release_threshold = 2;
    acs3.ProposerWhiteList proposer_white_list = 3;
}
```
- **CreateOrganizationInput**:
  - **token symbol**: not used in this contract.
  - **ProposalReleaseThreshold**:
    - **minimal approval threshold**: the new value for the minimum approval threshold.
    - **maximal rejection threshold**: the new value for the maximal rejection threshold.
    - **maximal abstention threshold**: he new value for the maximal abstention threshold.
    - **minimal vote threshold**: the new value for the minimal vote threshold.
  - **ProposerWhiteList**:
    - **proposers**: the new value for the list.


Creates an organization and returns its address.

## **ChangeOrganizationMember**

```Protobuf
rpc ChangeOrganizationMember(OrganizationMemberList) returns (google.protobuf.Empty) { }

message OrganizationMemberList {
    repeated aelf.Address organization_members = 1;
}
```

Changes the members of the organization. Note that this will override the current list.

- **OrganizationMemberList**:
  - **organization_members**: the new members.

# View methods

## **GetOrganization**

```Protobuf
rpc GetOrganization (aelf.Address) returns (Organization) { }

message Organization {
    OrganizationMemberList organization_member_list = 1;
    acs3.ProposalReleaseThreshold proposal_release_threshold = 2;
    acs3.ProposerWhiteList proposer_white_list = 3;
    aelf.Address organization_address = 4;
    aelf.Hash organization_hash = 5;
}
```

Returns the organization with the specified address.

- **Organization**:
  - **member list**: original members of this organization.
  - **ProposalReleaseThreshold**:
    - **minimal approval threshold**: the new value for the minimum approval threshold.
    - **maximal rejection threshold**: the new value for the maximal rejection threshold.
    - **maximal abstention threshold**: he new value for the maximal abstention threshold.
    - **minimal vote threshold**: the new value for the minimal vote threshold.
  - **ProposerWhiteList**:
    - **proposers**: the new value for the list.
  - **address**: the organizations address.
  - **hash**: the organizations ID.


## **ACS3 specific methods**

## **CreateProposal**

```Protobuf
rpc CreateProposal (CreateProposalInput) returns (aelf.Hash) { }

message CreateProposalInput {
    string contract_method_name = 2;
    aelf.Address to_address = 3;
    bytes params = 4;
    google.protobuf.Timestamp expired_time = 5;
    aelf.Address organization_address = 6;
}
```

This method creates a proposal for which organization members can vote. When the proposal is released, a transaction will be sent to the specified contract.

**returns:** the ID of the newly created proposal.

**CreateProposalInput**:
- **contract method name**: the name of the method to call after release.
- **to address**: the address of the contract to call after release.
- **expiration**: the date at which this proposal will expire.
- **organization address**: the address of the organization.

## **Reject**

```Protobuf
    rpc Reject(aelf.Hash) returns (google.protobuf.Empty) { }
```

This method is called to rejecting the specified proposal.

**Hash**: the hash of the proposal.

## **Abstain**

```Protobuf
    rpc Abstain(aelf.Hash) returns (google.protobuf.Empty) { }
```

This method is called to abstain from the specified proposal.

**Hash**: the hash of the proposal.

## **Release**

```Protobuf
    rpc Release(aelf.Hash) returns (google.protobuf.Empty) { }
```

This method is called to release the specified proposal.

**Hash**: the hash of the proposal.

## **ChangeOrganizationThreshold**

```Protobuf
rpc ChangeOrganizationThreshold(ProposalReleaseThreshold) returns (google.protobuf.Empty) { }

message ProposalReleaseThreshold {
    int64 minimal_approval_threshold = 1;
    int64 maximal_rejection_threshold = 2;
    int64 maximal_abstention_threshold = 3;
    int64 minimal_vote_threshold = 4;
}
```

This method changes the thresholds associated with proposals. All fields will be overwritten by the input value and this will afects all current proposals of the organization. Note: only the organization can execute this through a proposal.

**ProposalReleaseThreshold**:
- **minimal approval threshold**: the new value for the minimum approval threshold.
- **maximal rejection threshold**: the new value for the maximal rejection threshold.
- **maximal abstention threshold**: he new value for the maximal abstention threshold.
- **minimal vote threshold**: the new value for the minimal vote threshold.

## **ChangeOrganizationProposerWhiteList**

```Protobuf
rpc ChangeOrganizationProposerWhiteList(ProposerWhiteList) returns (google.protobuf.Empty) { }

message ProposerWhiteList {
    repeated aelf.Address proposers = 1;
}
```

This method overrides the list of whitelisted proposers.

**ProposerWhiteList**:
- **proposers**: the new value for the list.

## **CreateProposalBySystemContract**

```Protobuf
rpc CreateProposalBySystemContract(CreateProposalBySystemContractInput) returns (aelf.Hash) { }

message CreateProposalBySystemContractInput {
    acs3.CreateProposalInput proposal_input = 1;
    aelf.Address origin_proposer = 2;
    string proposal_id_feedback_method = 3;
}
```

Used by system contracts to create proposals.

**CreateProposalBySystemContractInput**:
- **CreateProposalInput**: 
  - **contract method name**: the name of the method to call after release.
  - **to address**: the address of the contract to call after release.
  - **expiration**: the date at which this proposal will expire.
  - **organization address**: the address of the organization.
- **origin proposer**: the actor that trigger the call.
- **proposal id feedback method**: the feedback method, called by inline transaction after creating the proposal.

## **ClearProposal**

```Protobuf
    rpc ClearProposal(aelf.Hash) returns (google.protobuf.Empty) { }
```

Removes the specified proposal.

## **ValidateOrganizationExist**

```Protobuf
    rpc ValidateOrganizationExist(aelf.Address) returns (google.protobuf.BoolValue) { }
```

Checks the existence of an organization.

# View methods

## **GetProposal**

```Protobuf
rpc GetProposal(aelf.Hash) returns (ProposalOutput) { }

message ProposalOutput {
    aelf.Hash proposal_id = 1;
    string contract_method_name = 2;
    aelf.Address to_address = 3;
    bytes params = 4;
    google.protobuf.Timestamp expired_time = 5;
    aelf.Address organization_address = 6;
    aelf.Address proposer = 7;
    bool to_be_released = 8;
    int64 approval_count = 9;
    int64 rejection_count = 10;
    int64 abstention_count = 11;
}
```

Get the proposal with the given ID.

**CreateProposalBySystemContractInput**:
- **proposal id**: ID of the proposal.
- **method name**: the method that this proposal will call when being released.
- **to address**: the address of the target contract.
- **params**: the parameters of the release transaction.
- **organization address**: address of this proposals organization.
- **proposer**: address of the proposer of this proposal.
- **to be release**: indicates if this proposal is releasable.


## **ValidateProposerInWhiteList**

```Protobuf
rpc ValidateProposerInWhiteList(ValidateProposerInWhiteListInput) returns (google.protobuf.BoolValue) { }

message ValidateProposerInWhiteListInput {
    aelf.Address proposer = 1;
    aelf.Address organization_address = 2;
}
```

Checks if the proposer is whitelisted.

**ValidateProposerInWhiteListInput**:
- **proposer**: the address to search/check.
- **organization address**: address of the organization.

