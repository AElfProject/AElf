using System;
using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    // ReSharper disable InconsistentNaming
    public partial class Round
    {
        public long RoundId => RealTimeMinersInfo.Values.Select(bpInfo => bpInfo.ExpectMiningTime.Seconds).Sum();

        public MinerInRound GetEBPInfo()
        {
            return RealTimeMinersInfo.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }

        public DateTime GetEBPMiningTime()
        {
            return RealTimeMinersInfo.OrderBy(m => m.Value.Order).Last().Value.ExpectMiningTime.ToDateTime()
                .AddMilliseconds(GlobalConfig.AElfDPoSMiningInterval);
        }

        public MinerInRound GetFirstPlaceMinerInfo()
        {
            return RealTimeMinersInfo.FirstOrDefault().Value;
        }
    }
}