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
    }
}