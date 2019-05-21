using AElf.Types;

namespace AElf.Contracts.Vote
{
    public static class VoteExtensions
    {
        public static Hash GetHash(this VotingRegisterInput votingItemInput, Address sponsorAddress)
        {
            var input = votingItemInput.Clone();
            input.Options.Clear();
            return Hash.FromTwoHashes(Hash.FromMessage(input), Hash.FromMessage(sponsorAddress));
        }

        public static Hash GetHash(this VotingResult votingResult)
        {
            return Hash.FromMessage(new VotingResult
            {
                VotingItemId = votingResult.VotingItemId,
                SnapshotNumber = votingResult.SnapshotNumber
            });
        }
    }
}