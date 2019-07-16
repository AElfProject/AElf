using System;
using System.Collections.Generic;
using System.Linq;
using Acs4;
using AElf.Kernel;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public long RoundId =>
            RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

        public bool IsEmpty => RoundId == 0;

        /// <summary>
        /// Check the equality of time slots of miners.
        /// Also, the mining interval shouldn't be 0.
        /// </summary>
        /// <returns></returns>
        public ValidationResult CheckRoundTimeSlots()
        {
            var miners = RealTimeMinersInformation.Values.OrderBy(m => m.Order).ToList();
            if (miners.Count == 1)
            {
                // No need to check single node.
                return new ValidationResult {Success = true};
            }

            if (miners.Any(m => m.ExpectedMiningTime == null))
            {
                return new ValidationResult {Message = "Incorrect expected mining time."};
            }

            var baseMiningInterval =
                (miners[1].ExpectedMiningTime - miners[0].ExpectedMiningTime).Milliseconds();

            if (baseMiningInterval <= 0)
            {
                return new ValidationResult {Message = $"Mining interval must greater than 0.\n{this}"};
            }

            for (var i = 1; i < miners.Count - 1; i++)
            {
                var miningInterval =
                    (miners[i + 1].ExpectedMiningTime - miners[i].ExpectedMiningTime).Milliseconds();
                if (Math.Abs(miningInterval - baseMiningInterval) > baseMiningInterval)
                {
                    return new ValidationResult {Message = "Time slots are so different."};
                }
            }

            return new ValidationResult {Success = true};
        }

        public Hash GetHash(bool isContainPreviousInValue = true)
        {
            return Hash.FromRawBytes(GetCheckableRound(isContainPreviousInValue));
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
                (int) (firstTwoMiners[1].ExpectedMiningTime - firstTwoMiners[0].ExpectedMiningTime)
                .Milliseconds();
            return distance > 0 ? distance : -distance;
        }

        public bool IsTimeSlotPassed(string publicKey, Timestamp currentBlockTime)
        {
            var miningInterval = GetMiningInterval();
            if (!RealTimeMinersInformation.ContainsKey(publicKey)) return false;
            var minerInRound = RealTimeMinersInformation[publicKey];
            if (RoundNumber != 1)
            {
                return minerInRound.ExpectedMiningTime + new Duration {Seconds = miningInterval.Div(1000)} <
                       currentBlockTime;
            }

            var actualStartTimes = FirstMiner().ActualMiningTimes;
            if (actualStartTimes.Count == 0)
            {
                return false;
            }

            var actualStartTime = actualStartTimes.First();
            var runningTime = currentBlockTime - actualStartTime;
            var expectedOrder = runningTime.Seconds.Div(miningInterval.Div(1000)).Add(1);
            return minerInRound.Order < expectedOrder;
        }

        /// <summary>
        /// Actually the expected mining time of the miner whose order is 1.
        /// </summary>
        /// <returns></returns>
        public Timestamp GetStartTime()
        {
            return FirstMiner().ExpectedMiningTime;
        }

        public Hash CalculateSignature(Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in RealTimeMinersInformation)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }

            return Hash.FromTwoHashes(inValue,
                RealTimeMinersInformation.Values.Aggregate(Hash.Empty,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }

        public Hash CalculateInValue(Hash randomHash)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(new Int64Value {Value = RoundId}), randomHash);
        }

        public Timestamp GetExtraBlockMiningTime()
        {
            return RealTimeMinersInformation.OrderBy(m => m.Value.Order).Last().Value
                .ExpectedMiningTime
                .AddMilliseconds(GetMiningInterval());
        }

        public long GetMinedBlocks()
        {
            return RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }

        public bool IsTimeToChangeTerm(Round previousRound, Timestamp blockchainStartTimestamp,
            long termNumber, long timeEachTerm)
        {
            var minersCount = previousRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null);
            var minimumCount = minersCount.Mul(2).Div(3).Add(1);
            var approvalsCount = RealTimeMinersInformation.Values.Where(m => m.ActualMiningTimes.Any())
                .Select(m => m.ActualMiningTimes.Last())
                .Count(actualMiningTimestamp =>
                    IsTimeToChangeTerm(blockchainStartTimestamp, actualMiningTimestamp, termNumber, timeEachTerm));
            return approvalsCount >= minimumCount;
        }

        public MinerInRound FirstMiner()
        {
            return RealTimeMinersInformation.Count > 0
                ? RealTimeMinersInformation.Values.First(m => m.Order == 1)
                : new MinerInRound();
        }

        public Timestamp GetExpectedMiningTime(string publicKey)
        {
            return RealTimeMinersInformation.ContainsKey(publicKey)
                ? RealTimeMinersInformation[publicKey].ExpectedMiningTime
                : new Timestamp {Seconds = long.MaxValue};;
        }

        public int GetMiningOrder(string publicKey)
        {
            return RealTimeMinersInformation.ContainsKey(publicKey)
                ? RealTimeMinersInformation[publicKey].Order
                : int.MaxValue;
        }

        /// <summary>
        /// If daysEachTerm == 7:
        /// 1, 1, 1 => 0 != 1 - 1 => false
        /// 1, 2, 1 => 0 != 1 - 1 => false
        /// 1, 8, 1 => 1 != 1 - 1 => true => term number will be 2
        /// 1, 9, 2 => 1 != 2 - 1 => false
        /// 1, 15, 2 => 2 != 2 - 1 => true => term number will be 3.
        /// </summary>
        /// <param name="blockchainStartTimestamp"></param>
        /// <param name="termNumber"></param>
        /// <param name="blockProducedTimestamp"></param>
        /// <param name="timeEachTerm"></param>
        /// <returns></returns>
        private bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
            long termNumber, long timeEachTerm)
        {
            return (blockProducedTimestamp - blockchainStartTimestamp).Seconds.Div(timeEachTerm) != termNumber - 1;
        }

        private byte[] GetCheckableRound(bool isContainPreviousInValue = true)
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation.Clone())
            {
                var checkableMinerInRound = minerInRound.Value.Clone();
                checkableMinerInRound.EncryptedInValues.Clear();
                checkableMinerInRound.DecryptedPreviousInValues.Clear();
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