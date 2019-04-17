using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private int SystemThreshold(int reviewerCount)
        {
            return reviewerCount * 2 / 3;
        }

        private IEnumerable<Representative> GetRepresentatives()
        {
            ValidateConsensusContract();
            var miner = State.ConsensusContract.GetCurrentMiners.Call(new Empty());
            var representatives = miner.MinerList.PublicKeys.Select(publicKey => new Representative
            {
                PubKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                Weight = 1 // weight is for farther improvement
            });
            return representatives;
        }
        
        private bool CheckApprovals(Hash proposalId, double releaseThreshold)
        {
            ValidateProposalContract();
            var approved = State.ProposalContract.GetApprovedResult.Call(proposalId);

            var representatives = GetRepresentatives();

            // processing approvals 
            var validApprovalWeights = approved.Approvals.Aggregate(0, (weights, approval) =>
            {
                var reviewer = representatives.FirstOrDefault(r => r.PubKey.Equals(approval.PublicKey));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            return validApprovalWeights >= Math.Ceiling(releaseThreshold * representatives.Count());
        }
        
        private void ValidateProposalContract()
        {
            if (State.ProposalContract.Value != null)
                return;
            State.ProposalContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ProposalContractSystemName.Value);
        }
        
        private void ValidateConsensusContract()
        {
            if (State.ConsensusContract.Value != null)
                return;
            State.ProposalContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);
        }

        private Address CalculateOrganizationAddress(Hash organizationHash)
        {
            Address organizationAddress =
                Context.ConvertVirtualAddressToContractAddress(Hash.FromTwoHashes(Hash.FromMessage(Context.Self),
                    organizationHash));
            return organizationAddress;
        }
    }
}