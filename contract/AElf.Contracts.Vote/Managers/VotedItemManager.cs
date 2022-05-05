using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Vote.Managers
{
    public class VotedItemManager:IVotedItemManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Address, VotedItems> _votedItemMap;

        public VotedItemManager(CSharpSmartContractContext context, MappedState<Address, VotedItems> votedItemMap)
        {
            _context = context;
            _votedItemMap = votedItemMap;
        }
        
        public void UpdateVotedItems(Hash voteId, Hash votingItemId, Address voter)
        {
            var votedItems = _votedItemMap[voter] ?? new VotedItems();
            var voterItemIndex = votingItemId.ToHex();
            if (votedItems.VotedItemVoteIds.ContainsKey(voterItemIndex))
            {
                votedItems.VotedItemVoteIds[voterItemIndex].ActiveVotes.Add(voteId);
            }
            else
            {
                votedItems.VotedItemVoteIds[voterItemIndex] = new VotedIds {ActiveVotes = {voteId}};
            }

            votedItems.VotedItemVoteIds[voterItemIndex].WithdrawnVotes.Remove(voteId);
            _votedItemMap[voter] = votedItems;
        }

        public VotedItems WithdrawVotedItems(Hash voteId, Hash votingItemId, Address voter)
        {
            var votedItems = _votedItemMap[voter] ?? new VotedItems();
            var voterItemIndex = votingItemId.ToHex();
           
            votedItems.VotedItemVoteIds[voterItemIndex].ActiveVotes.Remove(voteId);
            votedItems.VotedItemVoteIds[voterItemIndex].WithdrawnVotes.Add(voteId);
            _votedItemMap[voter] = votedItems;

            return votedItems;
        }

        public VotedItems GetVotedItems(Address voter)
        {
            return _votedItemMap[voter];
        }
    }
}