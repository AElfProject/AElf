using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private List<Address> GetCurrentMinerList()
        {
            MaybeLoadConsensusContractAddress();
            var miner = State.ConsensusContract.GetCurrentMinerList.Call(new Empty());
            var members = miner.Pubkeys.Select(publicKey =>
                Address.FromPublicKey(publicKey.ToByteArray())).ToList();
            return members;
        }

        private void AssertAuthorizedProposer()
        {
            // It is a valid proposer if
            // authority check is disable,
            // or sender is in proposer white list,
            // or sender is GenesisContract && origin is in proposer white list (for new contract deployment)
            // or sender is one of miners.
            if (!State.ProposerAuthorityRequired.Value)
                return;
            
            if (ValidateAddressInWhiteList(Context.Sender))
                return;

            Assert(Context.Sender == State.GenesisContract.Value && ValidateAddressInWhiteList(Context.Origin),
                "Not authorized to propose.");
        }

        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization,
            IEnumerable<Address> currentRepresentatives)
        {
            var currentParliament = new HashSet<Address>(currentRepresentatives);
            var approvalsCollectedFromCurrentParliament =
                proposal.ApprovedRepresentatives.Count(a => currentParliament.Contains(a));
            // approved >= (threshold/max) * representativeCount
            return approvalsCollectedFromCurrentParliament * MaxThreshold >=
                   organization.ReleaseThreshold * currentParliament.Count;
        }

        private void MaybeLoadConsensusContractAddress()
        {
            if (State.ConsensusContract.Value != null)
                return;
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        }

        private void AssertSenderIsParliamentMember(List<Address> currentParliament)
        {
            Assert(currentParliament.Any(r => r.Equals(Context.Sender)), "Not authorized approval.");
        }

        private const int MaxThreshold = 10000;

        private bool Validate(Organization organization)
        {
            return organization.ReleaseThreshold > 0 && organization.ReleaseThreshold <= MaxThreshold;
        }

        private bool Validate(ProposalInfo proposal)
        {
            var validDestinationAddress = proposal.ToAddress != null;
            var validDestinationMethodName = !string.IsNullOrWhiteSpace(proposal.ContractMethodName);
            var validExpiredTime = proposal.ExpiredTime != null && Context.CurrentBlockTime < proposal.ExpiredTime;
            var hasOrganizationAddress = proposal.OrganizationAddress != null;
            return validDestinationAddress && validDestinationMethodName && validExpiredTime & hasOrganizationAddress;
        }

        private ProposalInfo GetValidProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Proposal not found.");
            Assert(Validate(proposal), "Invalid proposal.");
            return proposal;
        }

        private void AssertProposalNotYetApprovedBySender(ProposalInfo proposal)
        {
            Assert(!proposal.ApprovedRepresentatives.Contains(Context.Sender), "Already approved.");
        }

        private bool ValidateAddressInWhiteList(Address address)
        {
            var currentMinerList = GetCurrentMinerList();
            return State.ProposerWhiteList.Value.Proposers.Any(p => p == address) ||
                   currentMinerList.Any(m => m == address);
        }
    }
}