using AElf.Contracts.Vote.Managers;
using AElf.Contracts.Vote.Services;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        private IVoteItemManager GetVoteItemManager()
        {
            return new VoteItemManager(Context, State.VotingItems);
        }

        private IVoteRecordManager GetVoteRecordManager()
        {
            return new VoteRecordManager(Context,State.VotingRecords);
        }

        private IVoteResultManager GetVoteResultManager()
        {
            return new VoteResultManager(Context ,State.VotingResults);
        }

        private IVotedItemManager GetVotedItemManager()
        {
            return new VotedItemManager(Context, State.VotedItemsMap);
        }

        private IVoteService GetVoteService()
        {
            var voteItemManager = GetVoteItemManager();
            var voteRecordManager = GetVoteRecordManager();
            var voteResultManager = GetVoteResultManager();
            var votedItemManager = GetVotedItemManager();
            return new VoteService(Context,State.TokenContract,voteItemManager,voteRecordManager,voteResultManager,votedItemManager);
        }
    }
}