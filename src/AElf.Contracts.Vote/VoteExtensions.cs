using AElf.Common;
using AElf.Kernel;

namespace AElf.Contracts.Vote
{
    public static class VoteExtensions
    {
        public static Hash GetHash(this VotingEvent votingEvent)
        {
            return Hash.FromMessage(new VotingEvent
            {
                Sponsor = votingEvent.Sponsor,
                Topic = votingEvent.Topic
            });
        }

        public static Hash GetHash(this VotingResult votingResult)
        {
            return Hash.FromMessage(new VotingResult
            {
                Sponsor = votingResult.Sponsor,
                Topic = votingResult.Topic,
                EpochNumber = votingResult.EpochNumber
            });
        }
        
        public static HashList Concat(this HashList hashList1, HashList hashList2)
        {
            var hashes = hashList1.Values;
            hashes.AddRange(hashList2.Values);
            return new HashList {Values = {hashes}};
        }
    }
}