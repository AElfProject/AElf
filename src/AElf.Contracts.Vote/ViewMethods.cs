using AElf.Kernel;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        public override GetVotingRecordsOutput GetVotingRecords(GetVotingRecordsInput input)
        {
            var output = new GetVotingRecordsOutput();

            foreach (var id in input.Ids)
            {
                output.Records.Add(State.VotingRecords[id]);
            }

            return output;
        }
    }
}