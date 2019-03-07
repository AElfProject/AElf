using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.DPoS.Extensions
{
    // ReSharper disable InconsistentNaming
    public static class MinersExtensions
    {
        public static bool IsEmpty(this Miners miners)
        {
            return !miners.PublicKeys.Any();
        }

        public static Hash GetMinersHash(this Miners miners)
        {
            return Hash.FromMessage(miners.PublicKeys.OrderBy(p => p).ToMiners());
        }

        public static Round GenerateFirstRoundOfNewTerm(this Miners miners, int miningInterval, ulong currentRoundNumber = 0, ulong currentTermNumber = 0)
        {
            var dict = new Dictionary<string, int>();

            foreach (var miner in miners.PublicKeys)
            {
                dict.Add(miner, miner[0]);
            }

            var sortedMiners =
                (from obj in dict
                    orderby obj.Value descending
                    select obj.Key).ToList();

            var round = new Round();

            // The extra block producer of first round is totally randomized.
            var selected = new Random().Next(0, miners.PublicKeys.Count);
            
            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                if (i == selected)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }
                
                minerInRound.PublicKey = sortedMiners[i];
                minerInRound.Order = i + 1;
                // Signatures totally randomized.
                minerInRound.Signature = Hash.Generate();
                minerInRound.ExpectedMiningTime =
                    GetTimestampOfUtcNow((i * miningInterval) + miningInterval);
                minerInRound.PromisedTinyBlocks = 1;

                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }

            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;

            return round;
        }
        
        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        private static Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            var now = Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
            return now;
        }
    }
}