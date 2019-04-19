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