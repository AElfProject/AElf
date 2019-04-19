using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract
    {
        private bool IsReadyToRelease(Hash proposalId, Organization organization)
        {
            var approvedVoteAmount = State.ApprovedTokenAmount[proposalId];
            return approvedVoteAmount.Value >= organization.ReleaseThreshold;
        }
    }
}