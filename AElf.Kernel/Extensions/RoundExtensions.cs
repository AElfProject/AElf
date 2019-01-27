using System;
using System.Linq;
using AElf.Common;

namespace AElf.Kernel
{
    public static class RoundExtensions
    {
        public static MinerInRound GetEBPInfo(this Round round)
        {
            return round.RealTimeMinersInfo.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }

        public static DateTime GetEBPMiningTime(this Round round, int miningInterval)
        {
            return round.RealTimeMinersInfo.OrderBy(m => m.Value.ExpectedMiningTime.ToDateTime()).Last().Value
                .ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(miningInterval);
        }

        public static MinerInRound GetFirstPlaceMinerInfo(this Round round)
        {
            return round.RealTimeMinersInfo.FirstOrDefault().Value;
        }

        public static Round Supplement(this Round round, Round previousRound)
        {
            foreach (var minerInRound in round.RealTimeMinersInfo.Values)
            {
                if (minerInRound.OutValue != null)
                {
                    continue;
                }

                minerInRound.MissedTimeSlots += 1;

                var inValue = Hash.Generate();
                var outValue = Hash.FromMessage(inValue);

                minerInRound.OutValue = outValue;
                minerInRound.InValue = inValue;

                var signature = previousRound.CalculateSignature(inValue);
                minerInRound.Signature = signature;
            }

            return round;
        }

        public static Round SupplementForFirstRound(this Round round)
        {
            foreach (var minerInRound in round.RealTimeMinersInfo.Values)
            {
                if (minerInRound.InValue != null && minerInRound.OutValue != null)
                {
                    continue;
                }

                minerInRound.MissedTimeSlots += 1;

                var inValue = Hash.Generate();
                var outValue = Hash.FromMessage(inValue);

                minerInRound.OutValue = outValue;
                minerInRound.InValue = inValue;
            }

            return round;
        }

        public static Hash CalculateSignature(this Round round, Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in round.RealTimeMinersInfo)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }

            return Hash.FromTwoHashes(inValue,
                round.RealTimeMinersInfo.Values.Aggregate(Hash.Default,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }

        public static Hash GetMinersHash(this Round round)
        {
            return Hash.FromMessage(round.RealTimeMinersInfo.Values.Select(m => m.PublicKey).OrderBy(p => p)
                .ToMiners());
        }

        public static ulong GetMinedBlocks(this Round round)
        {
            return round.RealTimeMinersInfo.Values.Select(mi => mi.ProducedBlocks)
                .Aggregate<ulong, ulong>(0, (current, @ulong) => current + @ulong);
        }

        public static bool CheckWhetherMostMinersMissedTimeSlots(this Round round)
        {
            if (GlobalConfig.BlockProducerNumber == 1)
            {
                return false;
            }

            var missedMinersCount = 0;
            foreach (var minerInRound in round.RealTimeMinersInfo)
            {
                if (minerInRound.Value.LatestMissedTimeSlots == GlobalConfig.ForkDetectionRoundNumber)
                {
                    missedMinersCount++;
                }
            }

            return missedMinersCount >= (GlobalConfig.BlockProducerNumber - 1) * GlobalConfig.ForkDetectionRoundNumber;
        }
    }
}