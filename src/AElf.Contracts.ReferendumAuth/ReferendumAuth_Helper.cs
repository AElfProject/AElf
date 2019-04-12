using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferencedunAuthContract
    {
        private void CheckParliamentAddress()
        {
            if (State.ParliamentAuthContractAddress.Value.Value != null)
                return;
            State.ParliamentAuthContractAddress.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ParliamentAuthContractSystemName.Value);
        }
        
        private bool IsReadyToRelease(ProposalInfo proposalInfo)
        {
            var proposal = proposalInfo.Proposal;
            var approvedVoteAmount = State.ApprovedVoteAmount[proposalInfo.ProposalHash];
            return approvedVoteAmount.Value >= SystemThreshold(proposal);
        }
        
        private double SystemThreshold(Proposal proposal)
        {
            var supply = GetVoteTotalSupply();
            return Math.Ceiling(supply * (proposal.Level == ProposalLevel.Normal ? 0.15 : 0.25 ));
        }

        private long GetVoteTotalSupply()
        {
            if (State.VoteTokenInfo.Value == null)
                State.VoteTokenInfo.Value = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
                {
                    Symbol = ReferendumConsts.VoteTokenInfoName
                });
            return State.VoteTokenInfo.Value.Supply;
        }
    }
}