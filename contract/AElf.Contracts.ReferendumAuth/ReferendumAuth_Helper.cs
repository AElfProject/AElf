using AElf.Contracts.MultiToken;
using AElf.Types;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract
    {
        private bool IsReleaseThresholdReached(Hash proposalId, Organization organization)
        {
            var approvedVoteAmount = State.ApprovedTokenAmount[proposalId];
            return approvedVoteAmount >= organization.ReleaseThreshold;
        }

        private void ValidateTokenContract()
        {
            if (State.TokenContract.Value != null)
                return;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        }

        private void LockToken(LockInput lockInput)
        {
            ValidateTokenContract();
            State.TokenContract.Lock.Send(lockInput);
        }

        private void UnlockToken(UnlockInput unlockInput)
        {
            ValidateTokenContract();
            State.TokenContract.Unlock.Send(unlockInput);
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
            Assert(proposal != null, "Invalid proposal id.");
            Assert(Validate(proposal), "Invalid proposal.");
            return proposal;
        }
    }
}