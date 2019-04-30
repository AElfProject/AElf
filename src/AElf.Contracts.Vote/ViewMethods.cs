using AElf.Kernel;
using Vote;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        public override VotingRecords GetVotingRecords(GetVotingRecordsInput input)
        {
            var votingRecords = new VotingRecords();

            foreach (var id in input.Ids)
            {
                votingRecords.Records.Add(State.VotingRecords[id]);
            }

            return votingRecords;
        }
    }
}