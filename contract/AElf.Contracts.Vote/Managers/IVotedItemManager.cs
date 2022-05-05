using AElf.Types;

namespace AElf.Contracts.Vote.Managers
{
    public interface IVotedItemManager
    {
        void UpdateVotedItems(Hash voteId, Hash votingItemId, Address voter);
        
        VotedItems WithdrawVotedItems(Hash voteId, Hash votingItemId, Address voter);
        
    }
}