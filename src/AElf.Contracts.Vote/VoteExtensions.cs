using AElf.Kernel;
using Vote;

namespace AElf.Contracts.Vote
{
    public static class VoteExtensions
    {
        public static Hash GetHash(this VotingRegisterInput votingItemInput, Address sponsorAddress)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(votingItemInput), Hash.FromMessage(sponsorAddress));
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