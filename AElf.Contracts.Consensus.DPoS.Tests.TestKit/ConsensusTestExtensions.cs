using System;

namespace AElf.Contracts.Consensus.DPoS
{
    public static class ConsensusTestExtensions
    {
        public static DateTime GetRoundExpectedStartTime(this DateTime blockchainStartTime, int roundTotalMilliseconds,
            long roundNumber)
        {
            return blockchainStartTime.AddMilliseconds(roundTotalMilliseconds * (roundNumber - 1));
        }
    }
}