using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract
    {
        private bool IsReadyToRelease(Hash proposalId, Address organizationAddress)
        {
            var approvedVoteAmount = State.ApprovedVoteAmount[proposalId];
            return approvedVoteAmount.Value >= ReleaseThreshold(organizationAddress);
        }
        
        private long ReleaseThreshold(Address organizationAddress)
        {
            var organization = GetOrganization(organizationAddress);
            return organization.ReleaseThreshold;
        }
    }
}