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

        public override BoolValue ValidateAddressInProposerWhiteList(Address address)
        {
            return State.ProposerAuthorityRequired.Value
                ? new BoolValue {Value = ValidateAddressInWhiteList(address)}
                : new BoolValue {Value = true};
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
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyApproved(proposal)) 
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
                if (proposal == null || !Validate(proposal) || CheckSenderAlreadyApproved(proposal)) 
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

            Hash hash = Hash.FromTwoHashes(Hash.FromMessage(input), Context.TransactionId);
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
            Context.Fire(new ProposalCreated {ProposalId = hash});
            
            return hash;
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