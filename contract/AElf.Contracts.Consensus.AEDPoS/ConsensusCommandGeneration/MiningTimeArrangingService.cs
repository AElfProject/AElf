using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public static class MiningTimeArrangingService
        {
            public static Timestamp ArrangeMiningTimeBasedOnOffset(Timestamp currentBlockTime, int offset)
            {
                return currentBlockTime.AddMilliseconds(offset);
            }

            public static Timestamp ArrangeNormalBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                var miningTime = round.GetExpectedMiningTime(pubkey);
                return miningTime > currentBlockTime ? miningTime : currentBlockTime;
            }

            public static Timestamp ArrangeExtraBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                return round.ArrangeAbnormalMiningTime(pubkey, currentBlockTime);
            }
        }
    }
}