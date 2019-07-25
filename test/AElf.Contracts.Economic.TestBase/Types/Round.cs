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

        internal bool IsTimeSlotPassed(string publicKey, DateTime dateTime,
            out MinerInRound minerInRound)
        {
            minerInRound = null;
            var miningInterval = GetMiningInterval();
            if (!RealTimeMinersInformation.ContainsKey(publicKey)) return false;
            minerInRound = RealTimeMinersInformation[publicKey];
            return minerInRound.ExpectedMiningTime.ToDateTime().AddMilliseconds(miningInterval) <= dateTime;
        }

        /// <summary>
        /// Actually the expected mining time of the miner whose order is 1.
        /// </summary>
        /// <returns></returns>
        public DateTime GetStartTime()
        {
            return RealTimeMinersInformation.Values.First(m => m.Order == 1).ExpectedMiningTime.ToDateTime();
        }

        /// <summary>
        /// This method for now is able to handle the situation of a miner keeping offline so many rounds,
        /// by using missedRoundsCount.
        /// </summary>
        /// <param name="miningInterval"></param>
        /// <param name="missedRoundsCount"></param>
        /// <returns></returns>
        public Timestamp GetExpectedEndTime(int missedRoundsCount = 0, int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = GetMiningInterval();
            }

            var totalMilliseconds = TotalMilliseconds(miningInterval);
            return GetStartTime().AddMilliseconds(totalMilliseconds)
                // Arrange an ending time if this node missed so many rounds.
                .AddMilliseconds(missedRoundsCount * totalMilliseconds)
                .ToTimestamp();
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
        
        public MinerInRound GetExtraBlockProducerInformation()
        {
            return RealTimeMinersInformation.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }
        
        public DateTime GetExtraBlockMiningTime()
        {
            return RealTimeMinersInformation.OrderBy(m => m.Value.ExpectedMiningTime.ToDateTime()).Last().Value
                .ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(GetMiningInterval());
        }
        
        /// <summary>
        /// Maybe tune other miners' supposed order of next round,
        /// will record this purpose to their FinalOrderOfNextRound field.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public UpdateValueInput ExtractInformationToUpdateConsensus(string publicKey)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return null;
            }

            var minerInRound = RealTimeMinersInformation[publicKey];

            var tuneOrderInformation = RealTimeMinersInformation.Values
                .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
                .ToDictionary(m => m.Pubkey, m => m.FinalOrderOfNextRound);

            var decryptedPreviousInValues = RealTimeMinersInformation.Values.Where(v =>
                    v.Pubkey != publicKey && v.DecryptedPreviousInValues.ContainsKey(publicKey))
                .ToDictionary(info => info.Pubkey, info => info.DecryptedPreviousInValues[publicKey]);

            var minersPreviousInValues =
                RealTimeMinersInformation.Values.Where(info => info.PreviousInValue != null).ToDictionary(info => info.Pubkey,
                    info => info.PreviousInValue);

            return new UpdateValueInput
            {
                OutValue = minerInRound.OutValue,
                Signature = minerInRound.Signature,
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = RoundId,
                ProducedBlocks = minerInRound.ProducedBlocks,
                ActualMiningTime = minerInRound.ActualMiningTimes.First(),
                SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
                TuneOrderInformation = {tuneOrderInformation},
                EncryptedInValues = {minerInRound.EncryptedInValues},
                DecryptedPreviousInValues = {decryptedPreviousInValues},
                MinersPreviousInValues = {minersPreviousInValues}
            };
        }
        
        public long GetMinedBlocks()
        {
            return RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }
        
        private static int GetAbsModulus(long longValue, int intValue)
        {
            return Math.Abs((int) longValue % intValue);
        }
    }
}