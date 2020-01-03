using System.Linq;
using Acs3;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using CreateProposalInput = Acs3.CreateProposalInput;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract : ParliamentAuthContractContainer.ParliamentAuthContractBase
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
            var minerList = GetCurrentMinerList();

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
                ToBeReleased = Validate(proposal) && IsReleaseThresholdReached(proposal, organization, minerList)
            };
        }

        public override Address GetDefaultOrganizationAddress(Empty input)
        {
            Assert(State.Initialized.Value, "Not initialized.");
            return State.DefaultOrganizationAddress.Value;
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

        public override ProposalIdList GetValidProposals(ProposalIdList input)
        {
            var result = new ProposalIdList();
            foreach (var proposalId in input.ProposalIds)
            {
                var proposal = State.Proposals[proposalId];
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyApproved(proposal, Context.Sender))
                    continue;
                result.ProposalIds.Add(proposalId);
            }

            return result;
        }

        public override ProposalIdList GetNotApprovedProposals(ProposalIdList input)
        {
            var result = new ProposalIdList();
            var currentParliament = GetCurrentMinerList();
            foreach (var proposalId in input.ProposalIds)
            {
                var proposal = State.Proposals[proposalId];
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyApproved(proposal, Context.Sender))
                    continue;
                var organization = State.Organisations[proposal.OrganizationAddress];
                if (organization == null || IsReleaseThresholdReached(proposal, organization, currentParliament))
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
            var organizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = input.GenesisOwnerReleaseThreshold
            };

            var proposerWhiteList = new ProposerWhiteList();

            if (input.PrivilegedProposer != null)
                proposerWhiteList.Proposers.Add(input.PrivilegedProposer);

            State.ProposerWhiteList.Value = proposerWhiteList;

            var defaultOrganizationAddress = CreateOrganization(organizationInput);
            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.DefaultOrganizationAddress.Value = defaultOrganizationAddress;
            State.GenesisContract.ChangeGenesisOwner.Send(defaultOrganizationAddress);
            State.ProposerAuthorityRequired.Value = input.ProposerAuthorityRequired;

            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            AssertAuthorizedProposer();
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            var organization = new Organization
            {
                ReleaseThreshold = input.ReleaseThreshold,
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
            AssertAuthorizedProposer();
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

        public override BoolValue Approve(ApproveInput approvalInput)
        {
            var proposal = GetValidProposal(approvalInput.ProposalId);
            AssertProposalNotYetApprovedBySender(proposal);
            AssertSenderIsParliamentMember();

            proposal.ApprovedRepresentatives.Add(Context.Sender);
            State.Proposals[approvalInput.ProposalId] = proposal;

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = GetValidProposal(proposalId);
            Assert(Context.Sender.Equals(proposalInfo.Proposer), "Unable to release this proposal.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            var currentParliament = GetCurrentMinerList();
            Assert(IsReleaseThresholdReached(proposalInfo, organization, currentParliament), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);
            Context.Fire(new ProposalReleased {ProposalId = proposalId});
            State.Proposals.Remove(proposalId);

            return new Empty();
        }

        public override Empty ApproveMultiProposals(ProposalIdList input)
        {
            AssertCurrentMiner();
            foreach (var proposalId in input.ProposalIds)
            {
                Approve(new ApproveInput {ProposalId = proposalId});
                Context.LogDebug(() => $"Proposal {proposalId} approved by {Context.Sender}");
            }

            return new Empty();
        }
    }
}