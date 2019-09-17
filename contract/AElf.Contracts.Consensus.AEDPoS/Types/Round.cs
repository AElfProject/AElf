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
        public Timestamp GetRoundStartTime()
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

        private Timestamp GetExtraBlockMiningTime()
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

        public int GetMiningOrder(string publicKey)
        {
            return RealTimeMinersInformation.ContainsKey(publicKey)
                ? RealTimeMinersInformation[publicKey].Order
                : int.MaxValue;
        }

        public bool TryToDetectEvilMiners(out List<string> evilMiners)
        {
            evilMiners = RealTimeMinersInformation.Values
                .Where(m => m.MissedTimeSlots >= AEDPoSContractConstants.MaximumMissedBlocksCount)
                .Select(m => m.Pubkey).ToList();
            return evilMiners.Count > 0;
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
            return (int) Math.Abs(longValue % intValue);
        }
    }
}