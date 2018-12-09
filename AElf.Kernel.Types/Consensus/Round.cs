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

        public MinerInRound GetEBPInfoForNextRound(int miningInterval)
        {
            var info = GetEBPInfo().Clone();
            info.Order = 1;
            info.RoundNumber += 1;
            info.Signature = null;
            info.OutValue = null;
            info.InValue = null;
            info.ExpectedMiningTime = DateTime.UtcNow.AddMilliseconds(miningInterval).ToTimestamp();

            return info;
        }

        public DateTime GetEBPMiningTime()
        {
            return RealTimeMinersInfo.OrderBy(m => m.Value.Order).Last().Value.ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(GlobalConfig.AElfDPoSMiningInterval);
        }

        public MinerInRound GetFirstPlaceMinerInfo()
        {
            return RealTimeMinersInfo.FirstOrDefault().Value;
        }

        public Round Supplement(Round previousRound)
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

                var signature = previousRound.CalculateSignature(inValue);
                minerInRound.Signature = signature;
            }

            return this;
        }

        public Hash CalculateSignature(Hash inValue)
        {
            return Hash.FromTwoHashes(inValue,
                RealTimeMinersInfo.Values.Aggregate(Hash.Default,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }
    }
}