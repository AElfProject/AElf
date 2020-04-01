using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private static class MiningTimeArrangingService
        {
            public static Timestamp ArrangeMiningTimeWithOffset(Timestamp currentBlockTime, int offset)
            {
                return currentBlockTime.AddMilliseconds(offset);
            }

            public static Timestamp ArrangeNormalBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                return TimestampExtensions.Max(round.GetExpectedMiningTime(pubkey), currentBlockTime);
            }

            public static Timestamp ArrangeExtraBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                return round.ArrangeAbnormalMiningTime(pubkey, currentBlockTime);
            }
        }
    }
}