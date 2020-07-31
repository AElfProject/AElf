using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    internal partial class Round
    {
        public long RoundId =>
            RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        public bool IsEmpty => RoundId == 0;

        public Hash GetHash(bool isContainPreviousInValue = true)
        {
            return HashHelper.ComputeFrom(GetCheckableRound(isContainPreviousInValue));
        }
        
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

        public bool IsTimeSlotPassed(string publicKey, DateTime dateTime,
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
        /// <param name="round"></param>
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

        public long GetMinedBlocks()
        {
            return RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }

        private byte[] GetCheckableRound(bool isContainPreviousInValue = true)
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation.Clone())
            {
                var checkableMinerInRound = minerInRound.Value.Clone();
                checkableMinerInRound.EncryptedPieces.Clear();
                checkableMinerInRound.ActualMiningTimes.Clear();
                if (!isContainPreviousInValue)
                {
                    checkableMinerInRound.PreviousInValue = Hash.Empty;
                }

                minersInformation.Add(minerInRound.Key, checkableMinerInRound);
            }

            var checkableRound = new Round
            {
                RoundNumber = RoundNumber,
                TermNumber = TermNumber,
                RealTimeMinersInformation = {minersInformation},
                BlockchainAge = BlockchainAge
            };
            return checkableRound.ToByteArray();
        }
        
        private static int GetAbsModulus(long longValue, int intValue)
        {
            return Math.Abs((int) longValue % intValue);
        }
    }
}