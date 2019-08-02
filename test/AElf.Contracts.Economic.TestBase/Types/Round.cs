using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    internal partial class Round
    {
        public long RoundId =>
            RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        /// <summary>
        /// This method is only available when the miners of this round is more than 1.
        /// </summary>
        /// <returns></returns>
        public int GetMiningInterval()
        {
            if (RealTimeMinersInformation.Count == 1)
            {
                // Just appoint the mining interval for single miner.
                return 4000;
            }

            var firstTwoMiners = RealTimeMinersInformation.Values.Where(m => m.Order == 1 || m.Order == 2)
                .ToList();
            var distance =
                (int) (firstTwoMiners[1].ExpectedMiningTime.ToDateTime() -
                       firstTwoMiners[0].ExpectedMiningTime.ToDateTime())
                .TotalMilliseconds;
            return distance > 0 ? distance : -distance;
        }

        /// <summary>
        /// In current AElf Consensus design, each miner produce his block in one time slot, then the extra block producer
        /// produce a block to terminate current round and confirm the mining order of next round.
        /// So totally, the time of one round is:
        /// MiningInterval * MinersCount + MiningInterval.
        /// </summary>
        /// <param name="miningInterval"></param>
        /// <returns></returns>                                                
        public int TotalMilliseconds(int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = GetMiningInterval();
            }

            return RealTimeMinersInformation.Count * miningInterval + miningInterval;
        }
        
        private static int GetAbsModulus(long longValue, int intValue)
        {
            return Math.Abs((int) longValue % intValue);
        }
    }
}