using AElf.Types;

namespace AElf.Contracts.Vote
{
    public static class VoteExtensions
    {
        public static Hash GetHash(this VotingRegisterInput votingItemInput, Address sponsorAddress)
        {
            var input = votingItemInput.Clone();
            input.Options.Clear();
            return HashHelper.ConcatAndCompute(Hash.ComputeFrom(input), Hash.ComputeFrom(sponsorAddress));
        }

        public static Hash GetHash(this VotingResult votingResult)
        {
            return Hash.ComputeFrom(new VotingResult
            {
                VotingItemId = votingResult.VotingItemId,
                SnapshotNumber = votingResult.SnapshotNumber
            });
        }
    }
}