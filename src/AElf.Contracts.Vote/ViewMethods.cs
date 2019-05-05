using System.Linq;
using AElf.Kernel;
using Vote;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        public override VotingRecords GetVotingRecords(GetVotingRecordsInput input)
        {
            var votingRecords = new VotingRecords();
            votingRecords.Records.AddRange(input.Ids.Select(id => State.VotingRecords[id]));
            return votingRecords;
        }

        public override VotedItems GetVotedItems(Address input)
        {
            return State.VotedItemsMap[input] ?? new VotedItems();
        }

        public override VotingRecord GetVotingRecord(Hash input)
        {
            var votingRecord = State.VotingRecords[input];
            Assert(votingRecord != null, "Voting record not found.");
            return votingRecord;
        }


        public override VotingItem GetVotingItem(GetVotingItemInput input)
        {
            var votingEvent = State.VotingItems[input.VotingItemId];
            Assert(votingEvent != null, "Voting item not found.");
            return votingEvent;
        }

        public override VotingResult GetVotingResult(GetVotingResultInput input)
        {
            var votingResultHash = new VotingResult
            {
                VotingItemId = input.VotingItemId,
                SnapshotNumber = input.SnapshotNumber
            }.GetHash();
            return State.VotingResults[votingResultHash];
        }

        public override VotedIds GetVotingIds(GetVotingIdsInput input)
        {
            return State.VotedItemsMap[input.Voter].VotedItemVoteIds.Where(p => p.Key == input.VotingItemId.ToHex())
                .Select(p => p.Value).First();
        }
    }
}