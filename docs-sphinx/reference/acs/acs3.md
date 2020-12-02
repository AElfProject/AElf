# ACS3 - Contract Proposal Standard

ACS3 is suitable for the case that a method needs to be approved by multiple parties. At this time, you can consider using some of the interfaces provided by ACS3.

## Interface

If you want multiple addresses vote to get agreement to do something, you can implement the following methods defined in ACS3:

* ``CreateProposal`` is to create a new proposal given target contract method and parameters. Therefore, the parameter CreateProposalInput defines the basic information of the inline transaction to be executed finally. The return value is a hash, which is used to uniquely identify this proposal;
* Approve, Reject, Abstain - are used to agree, reject, and abstain respectively.
* ``Release`` the parameter is the proposal Id, is used to release the proposal. If the release requirements are met, it is going to be released: use a virtual address as a Sender, and execute this method by sending an inline transaction. 
* ``ClearProposal`` used to clean expired proposal.

It can be seen that before a proposal is released, the account with voting rights can agree, reject, and abstain. Which specific accounts have the right to vote? ACS3 introduces the concept of **Organization**. A proposal is attached to an organization from its creation, and only members of the organization can vote.

However, due to the different forms of organization, the Organization structure needs to be defined by the contract implementing the ACS3. Here is an example:

```proto
message Organization{
    // The organization members.
    OrganizationMemberList organization_member_list = 1;
    // The threshold for releasing the proposal.
    acs3.ProposalReleaseThreshold proposal_release_threshold = 2;
    // The proposer whitelist.
    acs3.ProposerWhiteList proposer_white_list = 3;
    // The address of organization.
    aelf.Address organization_address = 4;
    // The organizations id.
    aelf.Hash organization_hash = 5;
    // The creation token is for organization address generation.
    aelf.Hash creation_token = 6;
}
```

Because each organization has a unique **organization_address**, sender authority can be verified very easily.

```c#
Assert(Context.Sender == someOrganization.OrganizationAddress, "No permission.");
```

How to know what an orgnanization has agreed on a proposal? ACS3 defines a data structure ProposalReleaseThreshold:

```proto
message ProposalReleaseThreshold {
    int64 minimal_approval_threshold = 1;
    int64 maximal_rejection_threshold = 2;
    int64 maximal_abstention_threshold = 3;
    int64 minimal_vote_threshold = 4;
}
```

The organization determines how to deal with the proposal according to the data:

* minimal_approval_threshold - the minimal approval count for the proposal to be released.
* maximal_rejection_threshold - the maximal rejection count for the proposal to be released.
* maximal_abstention_threshold - the maximal abstention count for the proposal to be released.
* minimal_vote_threshold - the minimal total vote count for the proposal to be released.

Interfaces referencing organization in ACS3:

* ``ChangeOrganizationThreshold``  used to modify the organization release threshold.
* ``ChangeOrganizationProposerWhiteList`` used to restrict which addresses can create proposals.
* ``CreateProposalBySystemContract`` used to create a proposal, which is only available to system contracts (built-in contracts)

The type of APIs mentioned above is action, there are some APIs with type View used to query:

* ``GetProposal`` used to get the proposal detailed information.
* ``ValidateOrganizationExist`` used to check if the organization exists in a contract.
* ``ValidateProposerInWhiteList`` used to check if the address is in the whitelist of an organization.

## Implementation

It is assumed here that there is only one organization in a contract, that is, there is no need to specifically define the Organization type.
Since the organization is not explicitly declared and created, the organization's proposal whitelist does not exist. The process here is 
that the voter must use a certain token to vote. 

For simplicity, only the core methods CreateProposal, Approve, Reject, Abstain, and Release are implemented here.

There are only two necessary State attributes:

```c#
public MappedState<Hash, ProposalInfo> Proposals { get; set; }
public SingletonState<ProposalReleaseThreshold> ProposalReleaseThreshold { get; set; }
```

The Proposals stores all proposal's information, and the ProposalReleaseThreshold is used to save the requirements that the contract needs to meet to release the proposal.

When the contract is initialized, the proposal release requirements should be set:

```c#
public override Empty Initialize(Empty input)
{
    State.TokenContract.Value =
        Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
    State.ProposalReleaseThreshold.Value = new ProposalReleaseThreshold
    {
        MinimalApprovalThreshold = 1,
        MinimalVoteThreshold = 1
    };
    return new Empty();
}
```

The requirement is at least one member who vote and at least one approval.
Create proposal:

```c#
public override Hash CreateProposal(CreateProposalInput input)
{
    var proposalId = Context.GenerateId(Context.Self, input.Token);
    Assert(State.Proposals[proposalId] == null, "Proposal with same token already exists.");
    State.Proposals[proposalId] = new ProposalInfo
    {
        ProposalId = proposalId,
        Proposer = Context.Sender,
        ContractMethodName = input.ContractMethodName,
        Params = input.Params,
        ExpiredTime = input.ExpiredTime,
        ToAddress = input.ToAddress,
        ProposalDescriptionUrl = input.ProposalDescriptionUrl
    };
    return proposalId;
}
```

Vote:

```c#
public override Empty Abstain(Hash input)
{
    Charge();
    var proposal = State.Proposals[input];
    if (proposal == null)
    {
        throw new AssertionException("Proposal not found.");
    }
    proposal.Abstentions.Add(Context.Sender);
    State.Proposals[input] = proposal;
    return new Empty();
}
public override Empty Approve(Hash input)
{
    Charge();
    var proposal = State.Proposals[input];
    if (proposal == null)
    {
        throw new AssertionException("Proposal not found.");
    }
    proposal.Approvals.Add(Context.Sender);
    State.Proposals[input] = proposal;
    return new Empty();
}
public override Empty Reject(Hash input)
{
    Charge();
    var proposal = State.Proposals[input];
    if (proposal == null)
    {
        throw new AssertionException("Proposal not found.");
    }
    proposal.Rejections.Add(Context.Sender);
    State.Proposals[input] = proposal;
    return new Empty();
}
private void Charge()
{
    State.TokenContract.TransferFrom.Send(new TransferFromInput
    {
        From = Context.Sender,
        To = Context.Self,
        Symbol = Context.Variables.NativeSymbol,
        Amount = 1_00000000
    });
}
```

Release is just count the vote, here is a recommended implementation:

```c#
public override Empty Release(Hash input)
{
    var proposal = State.Proposals[input];
    if (proposal == null)
    {
        throw new AssertionException("Proposal not found.");
    }
    Assert(IsReleaseThresholdReached(proposal), "Didn't reach release threshold.");
    Context.SendInline(proposal.ToAddress, proposal.ContractMethodName, proposal.Params);
    return new Empty();
}
private bool IsReleaseThresholdReached(ProposalInfo proposal)
{
    var isRejected = IsProposalRejected(proposal);
    if (isRejected)
        return false;
    var isAbstained = IsProposalAbstained(proposal);
    return !isAbstained && CheckEnoughVoteAndApprovals(proposal);
}
private bool IsProposalRejected(ProposalInfo proposal)
{
    var rejectionMemberCount = proposal.Rejections.Count;
    return rejectionMemberCount > State.ProposalReleaseThreshold.Value.MaximalRejectionThreshold;
}
private bool IsProposalAbstained(ProposalInfo proposal)
{
    var abstentionMemberCount = proposal.Abstentions.Count;
    return abstentionMemberCount > State.ProposalReleaseThreshold.Value.MaximalAbstentionThreshold;
}
private bool CheckEnoughVoteAndApprovals(ProposalInfo proposal)
{
    var approvedMemberCount = proposal.Approvals.Count;
    var isApprovalEnough =
        approvedMemberCount >= State.ProposalReleaseThreshold.Value.MinimalApprovalThreshold;
    if (!isApprovalEnough)
        return false;
    var isVoteThresholdReached =
        proposal.Abstentions.Concat(proposal.Approvals).Concat(proposal.Rejections).Count() >=
        State.ProposalReleaseThreshold.Value.MinimalVoteThreshold;
    return isVoteThresholdReached;
}
```

## Test

Before testing, two methods were added to a Dapp contract. We will test the proposal with these methods.

Define a singleton string and an organization address state in the State class:

```c#
public StringState Slogan { get; set; }
public SingletonState<Address> Organization { get; set; }
```

A pair of Set/Get methods:

```c#
public override StringValue GetSlogan(Empty input)
{
    return State.Slogan.Value == null ? new StringValue() : new StringValue {Value = State.Slogan.Value};
}

public override Empty SetSlogan(StringValue input)
{
    Assert(Context.Sender == State.Organization.Value, "No permission.");
    State.Slogan.Value = input.Value;
    return new Empty();
}
```

In this way, during the test, create a proposal for the SetSlogan. After passing and releasing, use the GetSlogan method to check whether the Slogan has been modified.

Prepare a Stub that implements the ACS3 contract:

```c#
var keyPair = SampleECKeyPairs.KeyPairs[0];
var acs3DemoContractStub =
    GetTester<ACS3DemoContractContainer.ACS3DemoContractStub>(DAppContractAddress, keyPair);
```

Since approval requires the contract to charge users, the user should send Approve transaction of the Token contract.

```c#
var tokenContractStub =
    GetTester<TokenContractContainer.TokenContractStub>(
        GetAddress(TokenSmartContractAddressNameProvider.StringName), keyPair);
await tokenContractStub.Approve.SendAsync(new ApproveInput
{
    Spender = DAppContractAddress,
    Symbol = "ELF",
    Amount = long.MaxValue
});
```

Create a proposal, the target method is SetSlogan, here we want to change the Slogan to "AElf" :

```c#
var proposalId = (await acs3DemoContractStub.CreateProposal.SendAsync(new CreateProposalInput
{
    OrganizationAddress = OrganizationAddress
    ContractMethodName = nameof(acs3DemoContractStub.SetSlogan),
    ToAddress = DAppContractAddress,
    ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
    Params = new StringValue {Value = "AElf"}.ToByteString(),
    Token = HashHelper.ComputeFrom("AElf")
})).Output;
```

Make sure that the Slogan is still an empty string at this time and then vote:

```c#
// Check slogan
{
    var slogan = await acs3DemoContractStub.GetSlogan.CallAsync(new Empty());
    slogan.Value.ShouldBeEmpty();
}
await acs3DemoContractStub.Approve.SendAsync(proposalId);
```

Release proposal, and the Slogan becomes "AElf".

```c#
await acs3DemoContractStub.Release.SendAsync(proposalId);
// Check slogan
{
    var slogan = await acs3DemoContractStub.GetSlogan.CallAsync(new Empty());
    slogan.Value.ShouldBe("AElf");
}
```
