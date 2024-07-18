using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament;

public partial class ParliamentContract
{
    private List<Address> GetCurrentMinerList()
    {
        RequireConsensusContractStateSet();
        var miner = State.ConsensusContract.GetCurrentMinerList.Call(new Empty());
        var members = miner.Pubkeys.Select(publicKey =>
            Address.FromPublicKey(publicKey.ToByteArray())).ToList();
        return members;
    }

    private void AssertIsAuthorizedProposer(Address organizationAddress, Address proposer)
    {
        var organization = State.Organizations[organizationAddress];
        Assert(organization != null, "No registered organization.");
        // It is a valid proposer if
        // authority check is disable,
        // or sender is in proposer white list,
        // or sender is one of miners when member proposing allowed.
        Assert(
            !organization.ProposerAuthorityRequired || ValidateAddressInWhiteList(proposer) ||
            (organization.ParliamentMemberProposingAllowed && ValidateParliamentMemberAuthority(proposer)),
            "Unauthorized to propose.");
    }

    private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
    {
        var parliamentMembers = GetCurrentMinerList();
        var isRejected = IsProposalRejected(proposal, organization, parliamentMembers);
        if (isRejected)
            return false;

        var isAbstained = IsProposalAbstained(proposal, organization, parliamentMembers);
        if (isAbstained)
            return false;

        return CheckEnoughVoteAndApprovals(proposal, organization, parliamentMembers);
    }

    private bool IsProposalStillPending(ProposalInfo proposal, Organization organization,
        ICollection<Address> parliamentMembers)
    {
        var isRejected = IsProposalRejected(proposal, organization, parliamentMembers);
        if (isRejected)
            return false;

        var isAbstained = IsProposalAbstained(proposal, organization, parliamentMembers);
        if (isAbstained)
            return false;

        return !CheckEnoughVoteAndApprovals(proposal, organization, parliamentMembers);
    }

    private bool IsProposalRejected(ProposalInfo proposal, Organization organization,
        ICollection<Address> parliamentMembers)
    {
        var rejectionMemberCount = proposal.Rejections.Count(parliamentMembers.Contains);
        return rejectionMemberCount * AbstractVoteTotal >
               organization.ProposalReleaseThreshold.MaximalRejectionThreshold * parliamentMembers.Count;
    }

    private bool IsProposalAbstained(ProposalInfo proposal, Organization organization,
        ICollection<Address> parliamentMembers)
    {
        var abstentionMemberCount = proposal.Abstentions.Count(parliamentMembers.Contains);
        return abstentionMemberCount * AbstractVoteTotal >
               organization.ProposalReleaseThreshold.MaximalAbstentionThreshold * parliamentMembers.Count;
    }

    private bool CheckEnoughVoteAndApprovals(ProposalInfo proposal, Organization organization,
        ICollection<Address> parliamentMembers)
    {
        var approvedMemberCount = proposal.Approvals.Count(parliamentMembers.Contains);
        var isApprovalEnough = approvedMemberCount * AbstractVoteTotal >=
                               organization.ProposalReleaseThreshold.MinimalApprovalThreshold *
                               parliamentMembers.Count;
        if (!isApprovalEnough)
            return false;

        var isVoteThresholdReached = IsVoteThresholdReached(proposal, organization, parliamentMembers);
        return isVoteThresholdReached;
    }

    private bool IsVoteThresholdReached(ProposalInfo proposal, Organization organization,
        ICollection<Address> parliamentMembers)
    {
        var isVoteThresholdReached =
            proposal.Abstentions.Concat(proposal.Approvals).Concat(proposal.Rejections)
                .Count(parliamentMembers.Contains) * AbstractVoteTotal >=
            organization.ProposalReleaseThreshold.MinimalVoteThreshold * parliamentMembers.Count;
        return isVoteThresholdReached;
    }

    private void RequireConsensusContractStateSet()
    {
        if (State.ConsensusContract.Value != null)
            return;
        State.ConsensusContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
    }

    private Address GetAndCheckActualParliamentMemberAddress()
    {
        var currentParliament = GetCurrentMinerList();

        if (currentParliament.Any(r => r.Equals(Context.Sender))) return Context.Sender;

        if (State.ElectionContract.Value == null)
        {
            var electionContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            if (electionContractAddress == null)
                // Election Contract not deployed - only possible in test environment.
                throw new AssertionException("Unauthorized sender.");

            State.ElectionContract.Value = electionContractAddress;
        }

        var managedPubkey = State.ElectionContract.GetManagedPubkeys.Call(Context.Sender);
        if (!managedPubkey.Value.Any()) throw new AssertionException("Unauthorized sender.");

        if (managedPubkey.Value.Count > 1)
            throw new AssertionException("Admin with multiple managed pubkeys cannot handle proposal.");

        var actualMemberAddress = Address.FromPublicKey(managedPubkey.Value.Single().ToByteArray());
        if (!currentParliament.Any(r => r.Equals(actualMemberAddress)))
            throw new AssertionException("Unauthorized sender.");

        return actualMemberAddress;
    }

    private bool Validate(Organization organization)
    {
        var proposalReleaseThreshold = organization.ProposalReleaseThreshold;

        return proposalReleaseThreshold.MinimalVoteThreshold <= AbstractVoteTotal &&
               proposalReleaseThreshold.MinimalApprovalThreshold <= proposalReleaseThreshold.MinimalVoteThreshold &&
               proposalReleaseThreshold.MinimalApprovalThreshold > 0 &&
               proposalReleaseThreshold.MaximalAbstentionThreshold >= 0 &&
               proposalReleaseThreshold.MaximalRejectionThreshold >= 0 &&
               proposalReleaseThreshold.MaximalAbstentionThreshold +
               proposalReleaseThreshold.MinimalApprovalThreshold <= AbstractVoteTotal &&
               proposalReleaseThreshold.MaximalRejectionThreshold +
               proposalReleaseThreshold.MinimalApprovalThreshold <= AbstractVoteTotal;
    }

    private bool Validate(ProposalInfo proposal)
    {
        var validDestinationAddress = proposal.ToAddress != null;
        var validDestinationMethodName = !string.IsNullOrWhiteSpace(proposal.ContractMethodName);
        var validExpiredTime = CheckProposalNotExpired(proposal);
        var hasOrganizationAddress = proposal.OrganizationAddress != null;
        var validDescriptionUrl = ValidateDescriptionUrlScheme(proposal.ProposalDescriptionUrl);
        return validDestinationAddress && validDestinationMethodName && validExpiredTime &&
               hasOrganizationAddress && validDescriptionUrl;
    }

    private bool ValidateDescriptionUrlScheme(string uriString)
    {
        if (string.IsNullOrEmpty(uriString))
            return true;
        var result = Uri.TryCreate(uriString, UriKind.Absolute, out var uriResult)
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return result;
    }

    private bool CheckProposalNotExpired(ProposalInfo proposal)
    {
        return proposal.ExpiredTime != null && Context.CurrentBlockTime < proposal.ExpiredTime;
    }

    private ProposalInfo GetValidProposal(Hash proposalId)
    {
        var proposal = State.Proposals[proposalId];
        Assert(proposal != null, "Proposal not found.");
        Assert(Validate(proposal), "Invalid proposal.");
        return proposal;
    }

    private void AssertProposalNotYetVotedByMember(ProposalInfo proposal, Address parliamentMemberAddress)
    {
        Assert(!CheckProposalAlreadyVotedBy(proposal, parliamentMemberAddress), "Already approved.");
    }

    private bool CheckProposalAlreadyVotedBy(ProposalInfo proposal, Address address)
    {
        return proposal.Approvals.Contains(address) || proposal.Rejections.Contains(address) ||
               proposal.Abstentions.Contains(address);
    }

    private bool ValidateAddressInWhiteList(Address address)
    {
        return State.ProposerWhiteList.Value.Proposers.Any(p => p == address);
    }

    private bool ValidateParliamentMemberAuthority(Address address)
    {
        var currentMinerList = GetCurrentMinerList();
        return currentMinerList.Any(m => m == address);
    }

    private void AssertCurrentMiner()
    {
        RequireConsensusContractStateSet();
        var isCurrentMiner = State.ConsensusContract.IsCurrentMiner.Call(Context.Sender).Value;
        Context.LogDebug(() => $"Sender is currentMiner : {isCurrentMiner}.");
        Assert(isCurrentMiner, "No permission.");
    }

    private Hash GenerateProposalId(CreateProposalInput input)
    {
        return Context.GenerateId(Context.Self, input.Token ?? HashHelper.ComputeFrom(input));
    }

    private Hash CreateNewProposal(CreateProposalInput input)
    {
        CheckCreateProposalInput(input);
        var proposalId = GenerateProposalId(input);
        var proposal = new ProposalInfo
        {
            ContractMethodName = input.ContractMethodName,
            ExpiredTime = input.ExpiredTime,
            Params = input.Params,
            ToAddress = input.ToAddress,
            OrganizationAddress = input.OrganizationAddress,
            ProposalId = proposalId,
            Proposer = Context.Sender,
            ProposalDescriptionUrl = input.ProposalDescriptionUrl,
            Title = input.Title,
            Description = input.Description
        };
        Assert(Validate(proposal), "Invalid proposal.");
        Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
        State.Proposals[proposalId] = proposal;
        Context.Fire(new ProposalCreated
        {
            ProposalId = proposalId, 
            OrganizationAddress = input.OrganizationAddress,
            Title = input.Title,
            Description = input.Description
        });
        return proposalId;
    }
    
    private void CheckCreateProposalInput(CreateProposalInput input)
    {
        // Check the length of title
        Assert(input.Title.Length <= ParliamentConstants.MaxLengthForTitle, "Title is too long.");
        // Check the length of description
        Assert(input.Description.Length <= ParliamentConstants.MaxLengthForDescription, "Description is too long.");
        // Check the length of description url
        Assert(input.ProposalDescriptionUrl.Length <= ParliamentConstants.MaxLengthForProposalDescriptionUrl,
            "Description url is too long.");
    }

    private Address CreateNewOrganization(CreateOrganizationInput input)
    {
        var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
        var organizationAddress = organizationHashAddressPair.OrganizationAddress;
        var organizationHash = organizationHashAddressPair.OrganizationHash;
        var organization = new Organization
        {
            ProposalReleaseThreshold = input.ProposalReleaseThreshold,
            OrganizationAddress = organizationAddress,
            OrganizationHash = organizationHash,
            ProposerAuthorityRequired = input.ProposerAuthorityRequired,
            ParliamentMemberProposingAllowed = input.ParliamentMemberProposingAllowed,
            CreationToken = input.CreationToken
        };
        Assert(Validate(organization), "Invalid organization.");
        if (State.Organizations[organizationAddress] != null)
            return organizationAddress;

        State.Organizations[organizationAddress] = organization;
        Context.Fire(new OrganizationCreated
        {
            OrganizationAddress = organizationAddress
        });

        return organizationAddress;
    }

    private OrganizationHashAddressPair CalculateOrganizationHashAddressPair(
        CreateOrganizationInput createOrganizationInput)
    {
        var organizationHash = HashHelper.ComputeFrom(createOrganizationInput);
        var organizationAddress =
            Context.ConvertVirtualAddressToContractAddressWithContractHashName(
                CalculateVirtualHash(organizationHash, createOrganizationInput.CreationToken));
        return new OrganizationHashAddressPair
        {
            OrganizationAddress = organizationAddress,
            OrganizationHash = organizationHash
        };
    }

    private Hash CalculateVirtualHash(Hash organizationHash, Hash creationToken)
    {
        return creationToken == null
            ? organizationHash
            : HashHelper.ConcatAndCompute(organizationHash, creationToken);
    }

    private void CreateEmergencyResponseOrganization()
    {
        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = 9000,
                MinimalVoteThreshold = 9000,
                MaximalAbstentionThreshold = 1000,
                MaximalRejectionThreshold = 1000
            },
            ProposerAuthorityRequired = false,
            ParliamentMemberProposingAllowed = true
        };

        State.EmergencyResponseOrganizationAddress.Value = CreateOrganization(createOrganizationInput);
    }
}