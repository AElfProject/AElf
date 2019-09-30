using Acs3;
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
            var readyToRelease = IsReleaseThresholdReached(proposal, organization, minerList);

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
                ToBeReleased = readyToRelease
            };
        }

        public override Address GetGenesisOwnerAddress(Empty input)
        {
            Assert(State.Initialized.Value, "Not initialized.");
            return State.GenesisOwnerAddress.Value;
        }

        #endregion view
        
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            var organizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = input.GenesisOwnerReleaseThreshold,
                ProposerAuthorityRequired = input.ProposerAuthorityRequired,
            };
            if (input.PrivilegedProposer != null)
                organizationInput.ProposerWhiteList.Add(input.PrivilegedProposer);
            
            State.GenesisOwnerAddress.Value = CreateOrganization(organizationInput);
            State.GenesisContract.Value = Context.GetZeroSmartContractAddress();
            State.GenesisContract.ChangeGenesisOwner.Send(State.GenesisOwnerAddress.Value);
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            var organization = new Organization
            {
                ReleaseThreshold = input.ReleaseThreshold,
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash,
                ProposerAuthorityRequired = input.ProposerAuthorityRequired,
                ProposerWhiteList = {input.ProposerWhiteList}
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
            AssertSenderIsAuthorizedProposer(organization);

            Hash hash = Hash.FromMessage(input);
            var proposal = new ProposalInfo
            {
                ContractMethodName = input.ContractMethodName,
                ExpiredTime = input.ExpiredTime,
                Params = input.Params,
                ToAddress = input.ToAddress,
                OrganizationAddress = input.OrganizationAddress,
                ProposalId = hash,
                Proposer = Context.Sender
            };
            Assert(Validate(proposal), "Invalid proposal.");
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = proposal;
            return hash;
        }

        public override BoolValue Approve(ApproveInput approvalInput)
        {
            var proposal = GetValidProposal(approvalInput.ProposalId);
            AssertProposalNotYetApprovedBySender(proposal);
            var currentParliament = GetCurrentMinerList();
            AssertSenderIsParliementMember(currentParliament);

            proposal.ApprovedRepresentatives.Add(Context.Sender);
            State.Proposals[approvalInput.ProposalId] = proposal;

            return new BoolValue {Value = true};
        }

        public override Empty Release(Hash proposalId)
        {
            var proposalInfo = State.Proposals[proposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            Assert(Context.Sender.Equals(proposalInfo.Proposer), "Unable to release this proposal.");
            var organization = State.Organisations[proposalInfo.OrganizationAddress];
            var currentParliament = GetCurrentMinerList();
            Assert(IsReleaseThresholdReached(proposalInfo, organization, currentParliament), "Not approved.");
            Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress,
                proposalInfo.ContractMethodName, proposalInfo.Params);
            
            return new Empty();
        }
    }
}