using System.Collections.Generic;
using System.Linq;
using AElf.Common;
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
            var miner = State.ConsensusContractReferenceState.GetCurrentMiners.Call(new Empty());
            var representatives = miner.MinerList.PublicKeys.Select(publicKey => new Representative
            {
                PubKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                Weight = 1 // weight is for farther improvement
            });
            return representatives;
        }
        
        private bool CheckApprovals(Hash proposalId)
        {
            var approved = State.Approved[proposalId];

            var toSig = proposalId.DumpByteArray();
            var representatives = GetRepresentatives();

            // processing approvals 
            var validApprovalWeights = approved.Approvals.Aggregate((int) 0, (weights, approval) =>
            {
                var reviewer = representatives.FirstOrDefault(r => r.PubKey.Equals(approval.PublicKey));
                if (reviewer == null)
                    return weights;
                return weights + reviewer.Weight;
            });

            return validApprovalWeights >= SystemThreshold(representatives.Count());
        }
    }
}