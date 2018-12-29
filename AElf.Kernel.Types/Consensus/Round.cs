using System;
using System.Linq;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    // ReSharper disable InconsistentNaming
    public partial class Round
    {
        public long RoundId => RealTimeMinersInfo.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        public MinerInRound GetEBPInfo()
        {
            return RealTimeMinersInfo.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }

        public DateTime GetEBPMiningTime(int miningInterval)
        {
            return RealTimeMinersInfo.OrderBy(m => m.Value.ExpectedMiningTime.ToDateTime()).Last().Value.ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(miningInterval);
        }

        public MinerInRound GetFirstPlaceMinerInfo()
        {
            return RealTimeMinersInfo.FirstOrDefault().Value;
        }

        public Round Supplement(Round previousRound)
        {
            foreach (var minerInRound in RealTimeMinersInfo.Values)
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

            return this;
        }

        public Round SupplementForFirstRound()
        {
            foreach (var minerInRound in RealTimeMinersInfo.Values)
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

            return this;
        }

        public Hash CalculateSignature(Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in RealTimeMinersInfo)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }
            
            return Hash.FromTwoHashes(inValue,
                RealTimeMinersInfo.Values.Aggregate(Hash.Default,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }

        public Hash MinersHash()
        {
            return Hash.FromMessage(RealTimeMinersInfo.Keys.ToMiners());
        }

        public ulong GetMinedBlocks()
        {
            return RealTimeMinersInfo.Values.Select(mi => mi.ProducedBlocks)
                .Aggregate<ulong, ulong>(0, (current, @ulong) => current + @ulong);
        }

        public bool CheckWhetherMostMinersMissedTimeSlots()
        {
            var missedMinersCount = 0;
            foreach (var minerInRound in RealTimeMinersInfo)
            {
                if (minerInRound.Value.LatestMissedTimeSlots == GlobalConfig.ForkDetectionRoundNumber)
                {
                    missedMinersCount++;
                }
            }

            return missedMinersCount >= GlobalConfig.BlockProducerNumber - 1;
        }
    }
}