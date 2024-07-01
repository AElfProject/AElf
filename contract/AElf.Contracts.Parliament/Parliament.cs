using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament;

public partial class ParliamentContract : ParliamentContractImplContainer.ParliamentContractImplBase
{
    public override Empty Initialize(InitializeInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");
        State.Initialized.Value = true;

        var proposerWhiteList = new ProposerWhiteList();

        if (input.PrivilegedProposer != null)
            proposerWhiteList.Proposers.Add(input.PrivilegedProposer);

        State.ProposerWhiteList.Value = proposerWhiteList;
        var organizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = DefaultOrganizationMinimalApprovalThreshold,
                MinimalVoteThreshold = DefaultOrganizationMinimalVoteThresholdThreshold,
                MaximalAbstentionThreshold = DefaultOrganizationMaximalAbstentionThreshold,
                MaximalRejectionThreshold = DefaultOrganizationMaximalRejectionThreshold
            },
            ProposerAuthorityRequired = input.ProposerAuthorityRequired,
            ParliamentMemberProposingAllowed = true
        };
        var defaultOrganizationAddress = CreateNewOrganization(organizationInput);
        State.DefaultOrganizationAddress.Value = defaultOrganizationAddress;
        return new Empty();
    }

    public override Address CreateOrganizationBySystemContract(CreateOrganizationBySystemContractInput input)
    {
        Assert(Context.GetSystemContractNameToAddressMapping().Values.Contains(Context.Sender),
            "Unauthorized to create organization.");
        var organizationAddress = CreateNewOrganization(input.OrganizationCreationInput);
        if (!string.IsNullOrEmpty(input.OrganizationAddressFeedbackMethod))
            Context.SendInline(Context.Sender, input.OrganizationAddressFeedbackMethod, organizationAddress);

        return organizationAddress;
    }

    public override Address CreateOrganization(CreateOrganizationInput input)
    {
        Assert(
            ValidateAddressInWhiteList(Context.Sender) || ValidateParliamentMemberAuthority(Context.Sender) ||
            State.DefaultOrganizationAddress.Value == Context.Sender,
            "Unauthorized to create organization.");
        var organizationAddress = CreateNewOrganization(input);

        return organizationAddress;
    }

    public override Hash CreateProposal(CreateProposalInput input)
    {
        AssertIsAuthorizedProposer(input.OrganizationAddress, Context.Sender);
        var proposalId = CreateNewProposal(input);
        return proposalId;
    }

    public override Hash CreateProposalBySystemContract(CreateProposalBySystemContractInput input)
    {
        Assert(Context.GetSystemContractNameToAddressMapping().Values.Contains(Context.Sender),
            "Unauthorized to propose.");
        AssertIsAuthorizedProposer(input.ProposalInput.OrganizationAddress, input.OriginProposer);

        var proposalId = CreateNewProposal(input.ProposalInput);
        return proposalId;
    }

    public override Empty Approve(Hash input)
    {
        var parliamentMemberAddress = GetAndCheckActualParliamentMemberAddress();
        var proposal = GetValidProposal(input);
        AssertProposalNotYetVotedByMember(proposal, parliamentMemberAddress);
        proposal.Approvals.Add(parliamentMemberAddress);
        State.Proposals[input] = proposal;
        Context.Fire(new ReceiptCreated
        {
            Address = parliamentMemberAddress,
            ProposalId = input,
            Time = Context.CurrentBlockTime,
            ReceiptType = nameof(Approve),
            OrganizationAddress = proposal.OrganizationAddress
        });
        return new Empty();
    }

    public override Empty Reject(Hash input)
    {
        var parliamentMemberAddress = GetAndCheckActualParliamentMemberAddress();
        var proposal = GetValidProposal(input);
        AssertProposalNotYetVotedByMember(proposal, parliamentMemberAddress);
        proposal.Rejections.Add(parliamentMemberAddress);
        State.Proposals[input] = proposal;
        Context.Fire(new ReceiptCreated
        {
            Address = parliamentMemberAddress,
            ProposalId = input,
            Time = Context.CurrentBlockTime,
            ReceiptType = nameof(Reject),
            OrganizationAddress = proposal.OrganizationAddress
        });
        return new Empty();
    }

    public override Empty Abstain(Hash input)
    {
        var parliamentMemberAddress = GetAndCheckActualParliamentMemberAddress();
        var proposal = GetValidProposal(input);
        AssertProposalNotYetVotedByMember(proposal, parliamentMemberAddress);
        proposal.Abstentions.Add(parliamentMemberAddress);
        State.Proposals[input] = proposal;
        Context.Fire(new ReceiptCreated
        {
            Address = parliamentMemberAddress,
            ProposalId = input,
            Time = Context.CurrentBlockTime,
            ReceiptType = nameof(Abstain),
            OrganizationAddress = proposal.OrganizationAddress
        });
        return new Empty();
    }

    public override Empty Release(Hash proposalId)
    {
        var proposalInfo = GetValidProposal(proposalId);
        Assert(Context.Sender.Equals(proposalInfo.Proposer), "No permission.");
        var organization = State.Organizations[proposalInfo.OrganizationAddress];
        Assert(IsReleaseThresholdReached(proposalInfo, organization), "Not approved.");
        Context.SendVirtualInlineBySystemContract(
            CalculateVirtualHash(organization.OrganizationHash, organization.CreationToken), proposalInfo.ToAddress,
            proposalInfo.ContractMethodName, proposalInfo.Params);
        Context.Fire(new ProposalReleased { ProposalId = proposalId });
        State.Proposals.Remove(proposalId);

        return new Empty();
    }

    public override Empty ChangeOrganizationThreshold(ProposalReleaseThreshold input)
    {
        var organization = State.Organizations[Context.Sender];
        Assert(organization != null, "Organization not found.");
        organization.ProposalReleaseThreshold = input;
        Assert(Validate(organization), "Invalid organization.");
        State.Organizations[Context.Sender] = organization;
        Context.Fire(new OrganizationThresholdChanged
        {
            OrganizationAddress = Context.Sender,
            ProposerReleaseThreshold = input
        });
        return new Empty();
    }

    public override Empty ChangeOrganizationProposerWhiteList(ProposerWhiteList input)
    {
        var defaultOrganizationAddress = State.DefaultOrganizationAddress.Value;
        Assert(defaultOrganizationAddress == Context.Sender, "No permission.");
        var organization = State.Organizations[defaultOrganizationAddress];
        Assert(
            input.Proposers.Count > 0 || !organization.ProposerAuthorityRequired ||
            organization.ParliamentMemberProposingAllowed, "White list can't be empty.");
        State.ProposerWhiteList.Value = input;
        Context.Fire(new OrganizationWhiteListChanged
        {
            OrganizationAddress = Context.Sender,
            ProposerWhiteList = input
        });
        return new Empty();
    }

    public override Empty ClearProposal(Hash input)
    {
        // anyone can clear proposal if it is expired
        var proposal = State.Proposals[input];
        Assert(proposal != null && Context.CurrentBlockTime >= proposal.ExpiredTime, "Proposal clear failed");
        State.Proposals.Remove(input);
        return new Empty();
    }

    public override Empty ApproveMultiProposals(ProposalIdList input)
    {
        AssertCurrentMiner();
        foreach (var proposalId in input.ProposalIds)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null || !CheckProposalNotExpired(proposal))
                continue;
            Approve(proposalId);
            Context.LogDebug(() => $"Proposal {proposalId} approved by {Context.Sender}");
        }

        return new Empty();
    }

    public override Empty CreateEmergencyResponseOrganization(Empty input)
    {
        Assert(State.EmergencyResponseOrganizationAddress.Value == null,
            "Emergency Response Organization already exists.");
        AssertSenderAddressWith(State.DefaultOrganizationAddress.Value);
        CreateEmergencyResponseOrganization();
        return new Empty();
    }

    public override Address GetEmergencyResponseOrganizationAddress(Empty input)
    {
        return State.EmergencyResponseOrganizationAddress.Value;
    }

    #region View

    public override Organization GetOrganization(Address address)
    {
        var organization = State.Organizations[address];
        return organization ?? new Organization();
    }

    public override ProposalOutput GetProposal(Hash proposalId)
    {
        var proposal = State.Proposals[proposalId];
        if (proposal == null) return new ProposalOutput();

        var organization = State.Organizations[proposal.OrganizationAddress];

        return new ProposalOutput
        {
            ProposalId = proposalId,
            ContractMethodName = proposal.ContractMethodName,
            ExpiredTime = proposal.ExpiredTime,
            OrganizationAddress = proposal.OrganizationAddress,
            Params = proposal.Params,
            Proposer = proposal.Proposer,
            ToAddress = proposal.ToAddress,
            ToBeReleased = Validate(proposal) && IsReleaseThresholdReached(proposal, organization),
            ApprovalCount = proposal.Approvals.Count,
            RejectionCount = proposal.Rejections.Count,
            AbstentionCount = proposal.Abstentions.Count,
            Title = proposal.Title,
            Description = proposal.Description
        };
    }

    public override Address GetDefaultOrganizationAddress(Empty input)
    {
        Assert(State.Initialized.Value, "Not initialized.");
        return State.DefaultOrganizationAddress.Value;
    }

    public override Address CalculateOrganizationAddress(CreateOrganizationInput input)
    {
        var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
        var organizationAddress = organizationHashAddressPair.OrganizationAddress;
        return organizationAddress;
    }

    public override BoolValue ValidateAddressIsParliamentMember(Address address)
    {
        return new BoolValue { Value = ValidateParliamentMemberAuthority(address) };
    }

    public override BoolValue ValidateProposerInWhiteList(ValidateProposerInWhiteListInput input)
    {
        return new BoolValue { Value = ValidateAddressInWhiteList(input.Proposer) };
    }

    public override ProposerWhiteList GetProposerWhiteList(Empty input)
    {
        var res = new ProposerWhiteList();
        var whitelist = State.ProposerWhiteList.Value;
        res.Proposers.AddRange(whitelist.Proposers);
        return res;
    }

    public override BoolValue ValidateOrganizationExist(Address input)
    {
        return new BoolValue { Value = State.Organizations[input] != null };
    }

    public override ProposalIdList GetNotVotedProposals(ProposalIdList input)
    {
        var result = new ProposalIdList();
        foreach (var proposalId in input.ProposalIds)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null || !Validate(proposal) || CheckProposalAlreadyVotedBy(proposal, Context.Sender))
                continue;
            result.ProposalIds.Add(proposalId);
        }

        return result;
    }

    public override ProposalIdList GetNotVotedPendingProposals(ProposalIdList input)
    {
        var result = new ProposalIdList();
        var currentParliament = GetCurrentMinerList();
        foreach (var proposalId in input.ProposalIds)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null || !Validate(proposal) || CheckProposalAlreadyVotedBy(proposal, Context.Sender))
                continue;
            var organization = State.Organizations[proposal.OrganizationAddress];
            if (organization == null || !IsProposalStillPending(proposal, organization, currentParliament))
                continue;
            result.ProposalIds.Add(proposalId);
        }

        return result;
    }

    public override ProposalIdList GetReleaseThresholdReachedProposals(ProposalIdList input)
    {
        var result = new ProposalIdList();
        foreach (var proposalId in input.ProposalIds)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null || !Validate(proposal))
                continue;
            var organization = State.Organizations[proposal.OrganizationAddress];
            if (organization == null || !IsReleaseThresholdReached(proposal, organization))
                continue;
            result.ProposalIds.Add(proposalId);
        }

        return result;
    }
    
    public override ProposalIdList GetAvailableProposals(ProposalIdList input)
    {
        var result = new ProposalIdList();
        foreach (var proposalId in input.ProposalIds)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null || !Validate(proposal))
                continue;
            result.ProposalIds.Add(proposalId);
        }

        return result;
    }

    #endregion view
}