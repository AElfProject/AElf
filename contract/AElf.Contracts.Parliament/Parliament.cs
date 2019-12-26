using System.Linq;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;

namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract : ParliamentContractContainer.ParliamentContractBase
    {
        #region View

        public override Organization GetOrganization(Address address)
        {
            var organization = State.Organisations[address];
            return organization ?? new Organization();
        }

        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null)
            {
                return new ProposalOutput();
            }

            var organization = State.Organisations[proposal.OrganizationAddress];

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
                ToBeReleased = Validate(proposal) && IsReleaseThresholdReached(proposal, organization)
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
            return new BoolValue {Value = ValidateParliamentMemberAuthority(address)};
        }

        public override GetProposerWhiteListContextOutput GetProposerWhiteListContext(Empty input)
        {
            return new GetProposerWhiteListContextOutput
            {
                ProposerAuthorityRequired = State.ProposerAuthorityRequired.Value,
                Proposers = {State.ProposerWhiteList.Value.Proposers}
            };
        }

        public override BoolValue ValidateOrganizationExist(Address input)
        {
            return new BoolValue {Value = State.Organisations[input] != null};
        }

        public override ProposalIdList GetNotVotedProposals(ProposalIdList input)
        {
            var result = new ProposalIdList();
            foreach (var proposalId in input.ProposalIds)
            {
                var proposal = State.Proposals[proposalId];
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyVoted(proposal, Context.Sender))
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
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyVoted(proposal, Context.Sender))
                    continue;
                var organization = State.Organisations[proposal.OrganizationAddress];
                if (organization == null || !IsProposalStillPending(proposal, organization, currentParliament))
                    continue;
                result.ProposalIds.Add(proposalId);
            }

            return result;
        }

        #endregion view

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
                    MinimalApprovalThreshold = AbstractDefaultOrganizationMinimalApprovalThreshold,
                    MinimalVoteThreshold = AbstractDefaultOrganizationMinimalVoteThresholdThreshold,
                    MaximalAbstentionThreshold = AbstractDefaultOrganizationMaximalAbstentionThreshold,
                    MaximalRejectionThreshold = AbstractDefaultOrganizationMaximalRejectionThreshold
                }
            };
            var defaultOrganizationAddress = CreateOrganization(organizationInput);
            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.DefaultOrganizationAddress.Value = defaultOrganizationAddress;
            State.GenesisContract.ChangeGenesisOwner.Send(defaultOrganizationAddress);
            State.ProposerAuthorityRequired.Value = input.ProposerAuthorityRequired;

            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            AssertAuthorizedProposer(Context.Sender);
            var organizationHashAddressPair = CalculateOrganizationHashAddressPair(input);
            var organizationAddress = organizationHashAddressPair.OrganizationAddress;
            var organizationHash = organizationHashAddressPair.OrganizationHash;
            var organization = new Organization
            {
                ProposalReleaseThreshold = input.ProposalReleaseThreshold,
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash
            };
            Assert(Validate(organization), "Invalid organization.");
            if (State.Organisations[organizationAddress] == null)
            {
                State.Organisations[organizationAddress] = organization;
            }

            return organizationAddress;
        }

        public override Hash CreateProposal(CreateProposalInput input)
        {
            var organization = State.Organisations[input.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            AssertAuthorizedProposer(Context.Sender);
            var proposalId = CreateNewProposal(input);
            return proposalId;
        }

        public override Hash CreateProposalBySystemContract(CreateProposalBySystemContractInput input)
        {
            var organization = State.Organisations[input.ProposalInput.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            Assert(
                Context.GetSystemContractNameToAddressMapping().Values.Contains(Context.Sender) &&
                CheckProposerAuthorityIfNeeded(input.OriginProposer), "Not authorized to propose.");
            var proposalId = CreateNewProposal(input.ProposalInput);
            if (!string.IsNullOrEmpty(input.ProposalIdFeedbackMethod))
                Context.SendInline(Context.Sender, input.ProposalIdFeedbackMethod, proposalId); // proposal id feedback
            return proposalId;
        }

        public override Empty Approve(Hash input)
        {
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal);
            AssertSenderIsParliamentMember();

            proposal.Approvals.Add(Context.Sender);
            State.Proposals[input] = proposal;

            return new Empty();
        }

        public override Empty Reject(Hash input)
        {
            AssertSenderIsParliamentMember();
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal);
            proposal.Rejections.Add(Context.Sender);
            State.Proposals[input] = proposal;

            return new Empty();
        }

        public override Empty Abstain(Hash input)
        {
            AssertSenderIsParliamentMember();
            var proposal = GetValidProposal(input);
            AssertProposalNotYetVotedBySender(proposal);
            proposal.Abstentions.Add(Context.Sender);
            State.Proposals[input] = proposal;

            return new Empty();
        }

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = GetValidProposal(proposalId);
            Assert(Context.Sender.Equals(proposalInfo.Proposer), "No permission.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            Assert(IsReleaseThresholdReached(proposalInfo, organization), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);
            Context.Fire(new ProposalReleased {ProposalId = proposalId});
            State.Proposals.Remove(proposalId);

            return new Empty();
        }

        public override Empty ChangeOrganizationThreshold(ProposalReleaseThreshold input)
        {
            var organization = State.Organisations[Context.Sender];
            Assert(organization != null, "Organization not found.");
            organization.ProposalReleaseThreshold = input;
            State.Organisations[Context.Sender] = organization;
            return new Empty();
        }

        public override Empty ChangeOrganizationProposerWhiteList(ProposerWhiteList input)
        {
            Assert(State.DefaultOrganizationAddress.Value == Context.Sender, "No permission.");
            State.ProposerWhiteList.Value = input;
            return new Empty();
        }

        public override Empty ClearProposal(Hash input)
        {
            // anyone can clear proposal if it is expired
            var proposal = State.Proposals[input];
            Assert(proposal != null && Context.CurrentBlockTime <= proposal.ExpiredTime, "Proposal clear failed");
            State.Proposals.Remove(input);
            return new Empty();
        }

        public override Empty ApproveMultiProposals(ProposalIdList input)
        {
            AssertCurrentMiner();
            foreach (var proposalId in input.ProposalIds)
            {
                Approve(proposalId);
                Context.LogDebug(() => $"Proposal {proposalId} approved by {Context.Sender}");
            }

            return new Empty();
        }
    }
}