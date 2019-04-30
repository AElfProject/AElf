using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestBase
{
    internal static class BasicExtensions
    {
        public static Round GenerateFirstRoundOfNewTerm(this Miners miners, int miningInterval,
            DateTime currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
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

            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                // The first miner will be the extra block producer of first round of each term.
                if (i == 0)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.PublicKey = sortedMiners[i];
                minerInRound.Order = i + 1;
                minerInRound.ExpectedMiningTime =
                    currentBlockTime.AddMilliseconds((i * miningInterval) + miningInterval).ToTimestamp();
                minerInRound.PromisedTinyBlocks = 1;
                // Should be careful during validation.
                minerInRound.PreviousInValue = Hash.Empty;

                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }

            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;

            return round;
        }

        public static long GetMinedBlocks(this Round round)
        {
            return round.RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }
        public static Miners ToMiners(this List<string> minerPublicKeys, long termNumber = 0)
        {
            return new Miners
            {
                PublicKeys = {minerPublicKeys},
                Addresses = {minerPublicKeys.Select(p => Address.FromPublicKey(ByteArrayHelpers.FromHexString(p)))},
                TermNumber = termNumber
            };
        }

    }
}