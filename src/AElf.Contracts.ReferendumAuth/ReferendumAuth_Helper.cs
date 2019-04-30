using AElf.Contracts.MultiToken.Messages;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract
    {
        private bool IsReadyToRelease(Hash proposalId, Organization organization)
        {
            var approvedVoteAmount = State.ApprovedTokenAmount[proposalId];
            return approvedVoteAmount >= organization.ReleaseThreshold;
        }

        private void ValidateTokenContract()
        {
            if (State.TokenContract.Value != null)
                return;
            State.TokenContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
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
    }
}