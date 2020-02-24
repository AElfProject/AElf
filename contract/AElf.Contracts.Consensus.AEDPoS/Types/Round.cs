using System;
using System.Collections.Generic;
using System.Linq;
using Acs4;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public long RoundId
        {
            get
            {
                if (RealTimeMinersInformation.Values.All(bpInfo => bpInfo.ExpectedMiningTime != null))
                {
                    return RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();
                }

                return RoundIdForValidation;
            }
        }

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
                return new ValidationResult {Message = $"Incorrect expected mining time.\n{this}"};
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

        public string GetCurrentMinerPubkey(Timestamp currentBlockTime)
        {
            var pubkey = RealTimeMinersInformation.Values.OrderBy(m => m.Order).FirstOrDefault(m =>
                m.ExpectedMiningTime <= currentBlockTime &&
                currentBlockTime < m.ExpectedMiningTime.AddMilliseconds(GetMiningInterval()))?.Pubkey;
            if (pubkey != null)
            {
                return pubkey;
            }

            var extraBlockProducer = RealTimeMinersInformation.Values.First(m => m.IsExtraBlockProducer).Pubkey;
            var extraBlockMiningTime = GetExtraBlockMiningTime();
            if (extraBlockMiningTime <= currentBlockTime &&
                currentBlockTime <= extraBlockMiningTime.AddMilliseconds(GetMiningInterval()))
            {
                return extraBlockProducer;
            }

            return RealTimeMinersInformation.Keys.First(k => IsInCorrectFutureMiningSlot(k, currentBlockTime));
        }

        /// <summary>
        /// For multiple miners, return the length every mining time slot (which should be equal).
        /// For single miner, return 4000 ms.
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

            return Math.Abs((int) (firstTwoMiners[1].ExpectedMiningTime - firstTwoMiners[0].ExpectedMiningTime)
                .Milliseconds());
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
        public Timestamp GetRoundStartTime()
        {
            return FirstMiner().ExpectedMiningTime;
        }

        public Hash CalculateSignature(Hash inValue)
        {
            return HashHelper.Xor(inValue,
                RealTimeMinersInformation.Values.Aggregate(Hash.Empty,
                    (current, minerInRound) => HashHelper.Xor(current, minerInRound.Signature)));
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

        public MinerList GetMinerList()
        {
            return new MinerList
            {
                Pubkeys = {RealTimeMinersInformation.Keys.Select(k => k.ToByteString())}
            };
        }

        public bool IsInMinerList(string pubkey)
        {
            return RealTimeMinersInformation.Keys.Contains(pubkey);
        }

        public MinerInRound FirstMiner()
        {
            return RealTimeMinersInformation.Count > 0
                ? RealTimeMinersInformation.Values.FirstOrDefault(m => m.Order == 1)
                // Unlikely.
                : new MinerInRound();
        }

        public MinerInRound FirstActualMiner()
        {
            return RealTimeMinersInformation.Count > 0
                ? RealTimeMinersInformation.Values.FirstOrDefault(m => m.OutValue != null)
                : null;
        }

        public Timestamp GetExpectedMiningTime(string publicKey)
        {
            return RealTimeMinersInformation.ContainsKey(publicKey)
                ? RealTimeMinersInformation[publicKey].ExpectedMiningTime
                : new Timestamp {Seconds = long.MaxValue};
        }

        /// <summary>
        /// Get miner's order of provided round information.
        /// If provided round doesn't contain this pubkey, return int.MaxValue.
        /// </summary>
        /// <param name="pubkey"></param>
        /// <returns></returns>
        public int GetMiningOrder(string pubkey)
        {
            return RealTimeMinersInformation.ContainsKey(pubkey)
                ? RealTimeMinersInformation[pubkey].Order
                : int.MaxValue;
        }

        public bool TryToDetectEvilMiners(out List<string> evilMiners)
        {
            evilMiners = RealTimeMinersInformation.Values
                .Where(m => m.MissedTimeSlots >= AEDPoSContractConstants.TolerableMissedTimeSlotsCount)
                .Select(m => m.Pubkey).ToList();
            return evilMiners.Count > 0;
        }

        private byte[] GetCheckableRound(bool isContainPreviousInValue = true)
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation.Clone())
            {
                var checkableMinerInRound = minerInRound.Value.Clone();
                checkableMinerInRound.EncryptedPieces.Clear();
                checkableMinerInRound.DecryptedPieces.Clear();
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

        public Round GetSimpleRound()
        {
            var minersInformation = new Dictionary<string, MinerInRound>();
            foreach (var minerInRound in RealTimeMinersInformation.Clone())
            {
                var checkableMinerInRound = minerInRound.Value.Clone();
                checkableMinerInRound.EncryptedPieces.Clear();
                checkableMinerInRound.DecryptedPieces.Clear();
                checkableMinerInRound.ActualMiningTimes.Clear();
                minersInformation.Add(minerInRound.Key, checkableMinerInRound);
            }

            return new Round
            {
                RoundNumber = RoundNumber,
                RealTimeMinersInformation = {minersInformation},
            };
        }

        /// <summary>
        /// Change term if two thirds of miners latest ActualMiningTime meets threshold of changing term.
        /// </summary>
        /// <param name="blockchainStartTimestamp"></param>
        /// <param name="currentTermNumber"></param>
        /// <param name="timeEachTerm"></param>
        /// <returns></returns>
        public bool NeedToChangeTerm(Timestamp blockchainStartTimestamp, long currentTermNumber, long timeEachTerm)
        {
            return RealTimeMinersInformation.Values
                       .Where(m => m.ActualMiningTimes.Any())
                       .Select(m => m.ActualMiningTimes.Last())
                       .Count(t => IsTimeToChangeTerm(blockchainStartTimestamp,
                           t, currentTermNumber, timeEachTerm))
                   >= MinersCountOfConsent;
        }

        /// <summary>
        /// If timeEachTerm == 7:
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
        private static bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
            long termNumber, long timeEachTerm)
        {
            return (blockProducedTimestamp - blockchainStartTimestamp).Seconds.Div(timeEachTerm) != termNumber - 1;
        }

        private static int GetAbsModulus(long longValue, int intValue)
        {
            return (int) Math.Abs(longValue % intValue);
        }
    }
}