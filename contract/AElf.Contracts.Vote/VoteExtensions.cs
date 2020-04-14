using AElf.Types;

namespace AElf.Contracts.Vote
{
    public static class VoteExtensions
    {
        public static Hash GetHash(this VotingRegisterInput votingItemInput, Address sponsorAddress)
        {
            var input = votingItemInput.Clone();
            input.Options.Clear();
            return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(input), HashHelper.ComputeFrom(sponsorAddress));
        }

        public static Hash GetHash(this VotingResult votingResult)
        {
            return HashHelper.ComputeFrom(new VotingResult
            {
                VotingItemId = votingResult.VotingItemId,
                SnapshotNumber = votingResult.SnapshotNumber
            });
        }
    }
}