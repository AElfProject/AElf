using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Consensus.DPoS
{
    public static class BasicExtensions
    {
        public static bool IsEmpty(this Round round)
        {
            return round.RoundId == 0;
        }

        public static string GetLogs(this Round round, string publicKey, DPoSBehaviour behaviour)
        {
            var logs = new StringBuilder($"\n[Round {round.RoundNumber}](Round Id: {round.RoundId})[Term {round.TermNumber}]");
            foreach (var minerInRound in round.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = new StringBuilder("\n");
                minerInformation.Append($"[{minerInRound.PublicKey.Substring(0, 10)}]");
                minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
                minerInformation.AppendLine(minerInRound.PublicKey == publicKey
                    ? "(This Node)"
                    : "");
                minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
                minerInformation.AppendLine(
                    $"Expect:\t {minerInRound.ExpectedMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
                minerInformation.AppendLine(
                    $"Actual:\t {minerInRound.ActualMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
                minerInformation.AppendLine($"Out:\t {minerInRound.OutValue?.ToHex()}");
                if (round.RoundNumber != 1)
                {
                    minerInformation.AppendLine($"PreIn:\t {minerInRound.PreviousInValue?.ToHex()}");
                }

                minerInformation.AppendLine($"Sig:\t {minerInRound.Signature?.ToHex()}");
                minerInformation.AppendLine($"Mine:\t {minerInRound.ProducedBlocks}");
                minerInformation.AppendLine($"Miss:\t {minerInRound.MissedTimeSlots}");
                minerInformation.AppendLine($"Proms:\t {minerInRound.PromisedTinyBlocks}");
                minerInformation.AppendLine($"NOrder:\t {minerInRound.FinalOrderOfNextRound}");

                logs.Append(minerInformation);
            }

            logs.AppendLine($"Recent behaviour: {behaviour.ToString()}");

            return logs.ToString();
        }

        public static Hash CalculateInValue(this Round round, Hash randomHash)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(new Int64Value {Value = round.RoundId}), randomHash);
        }

        public static MinerInRound GetExtraBlockProducerInformation(this Round round)
        {
            return round.RealTimeMinersInformation.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }

        /// <summary>
        /// Maybe tune other miners' supposed order of next round,
        /// will record this purpose to their FinalOrderOfNextRound field.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static ToUpdate ExtractInformationToUpdateConsensus(this Round round, string publicKey)
        {
            if (!round.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return null;
            }

            var tuneOrderInformation = round.RealTimeMinersInformation.Values
                .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
                .ToDictionary(m => m.PublicKey, m => m.FinalOrderOfNextRound);

            var decryptedPreviousInValues = round.RealTimeMinersInformation.Values.Where(v =>
                    v.PublicKey != publicKey && v.DecryptedPreviousInValues.ContainsKey(publicKey))
                .ToDictionary(info => info.PublicKey, info => info.DecryptedPreviousInValues[publicKey]);

            var minersPreviousInValues =
                round.RealTimeMinersInformation.Values.Where(info => info.PreviousInValue != null).ToDictionary(info => info.PublicKey,
                    info => info.PreviousInValue);

            var minerInRound = round.RealTimeMinersInformation[publicKey];
            return new ToUpdate
            {
                OutValue = minerInRound.OutValue,
                Signature = minerInRound.Signature,
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = round.RoundId,
                PromiseTinyBlocks = minerInRound.PromisedTinyBlocks,
                ProducedBlocks = minerInRound.ProducedBlocks,
                ActualMiningTime = minerInRound.ActualMiningTime,
                SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
                TuneOrderInformation = {tuneOrderInformation},
                EncryptedInValues = {minerInRound.EncryptedInValues},
                DecryptedPreviousInValues = {decryptedPreviousInValues},
                MinersPreviousInValues = {minersPreviousInValues}
            };
        }

        public static Round ApplyNormalConsensusData(this Round round, string publicKey, Hash previousInValue,
            Hash outValue, Hash signature, DateTime dateTime)
        {
            if (!round.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return round;
            }

            round.RealTimeMinersInformation[publicKey].ActualMiningTime = dateTime.ToTimestamp();
            round.RealTimeMinersInformation[publicKey].OutValue = outValue;
            round.RealTimeMinersInformation[publicKey].Signature = signature;
            round.RealTimeMinersInformation[publicKey].ProducedBlocks += 1;
            if (previousInValue != Hash.Empty)
            {
                round.RealTimeMinersInformation[publicKey].PreviousInValue = previousInValue;
            }

            var minersCount = round.RealTimeMinersInformation.Count;
            var sigNum =
                BitConverter.ToInt64(
                    BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(),
                    0);
            var supposedOrderOfNextRound = GetAbsModulus(sigNum, minersCount) + 1;

            // Check the existence of conflicts about OrderOfNextRound.
            // If so, modify others'.
            var conflicts = round.RealTimeMinersInformation.Values
                .Where(i => i.FinalOrderOfNextRound == supposedOrderOfNextRound).ToList();

            foreach (var orderConflictedMiner in conflicts)
            {
                // Though multiple conflicts should be wrong, we can still arrange their orders of next round.

                for (var i = supposedOrderOfNextRound + 1; i < minersCount * 2; i++)
                {
                    var maybeNewOrder = i > minersCount ? i % minersCount : i;
                    if (round.RealTimeMinersInformation.Values.All(m => m.FinalOrderOfNextRound != maybeNewOrder))
                    {
                        round.RealTimeMinersInformation[orderConflictedMiner.PublicKey].FinalOrderOfNextRound =
                            maybeNewOrder;
                        break;
                    }
                }
            }

            round.RealTimeMinersInformation[publicKey].SupposedOrderOfNextRound = supposedOrderOfNextRound;
            // Initialize FinalOrderOfNextRound as the value of SupposedOrderOfNextRound
            round.RealTimeMinersInformation[publicKey].FinalOrderOfNextRound = supposedOrderOfNextRound;

            return round;
        }

        public static List<MinerInRound> GetMinedMiners(this Round round)
        {
            // For now only this implementation can support test cases.
            return round.RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound != 0).ToList();
        }
        
        public static List<MinerInRound> GetNotMinedMiners(this Round round)
        {
            // For now only this implementation can support test cases.
            return round.RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound == 0).ToList();
        }

        public static bool GenerateNextRoundInformation(this Round currentRound, DateTime dateTime,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            nextRound = new Round();

            var minersMinedCurrentRound = currentRound.GetMinedMiners();
            var minersNotMinedCurrentRound = currentRound.GetNotMinedMiners();
            var minersCount = currentRound.RealTimeMinersInformation.Count;

            var miningInterval = currentRound.GetMiningInterval();
            nextRound.RoundNumber = currentRound.RoundNumber + 1;
            nextRound.TermNumber = currentRound.TermNumber;
            if (currentRound.RoundNumber == 1)
            {
                nextRound.BlockchainAge = 1;
            }
            else
            {
                nextRound.BlockchainAge =
                    (long) (dateTime - blockchainStartTimestamp.ToDateTime())
                    .TotalMinutes; // TODO: Change to TotalDays after testing.
            }

            // Set next round miners' information of miners who successfully mined during this round.
            foreach (var minerInRound in minersMinedCurrentRound.OrderBy(m => m.FinalOrderOfNextRound))
            {
                var order = minerInRound.FinalOrderOfNextRound;
                nextRound.RealTimeMinersInformation[minerInRound.PublicKey] = new MinerInRound
                {
                    PublicKey = minerInRound.PublicKey,
                    Order = order,
                    ExpectedMiningTime = dateTime.ToTimestamp().GetArrangedTimestamp(order, miningInterval),
                    PromisedTinyBlocks = minerInRound.PromisedTinyBlocks,
                    ProducedBlocks = minerInRound.ProducedBlocks,
                    MissedTimeSlots = minerInRound.MissedTimeSlots
                };
            }

            // Set miners' information of miners missed their time slot in current round.
            var occupiedOrders = minersMinedCurrentRound.Select(m => m.FinalOrderOfNextRound).ToList();
            var ableOrders = Enumerable.Range(1, minersCount).Where(i => !occupiedOrders.Contains(i)).ToList();
            for (var i = 0; i < minersNotMinedCurrentRound.Count; i++)
            {
                var order = ableOrders[i];
                var minerInRound = minersNotMinedCurrentRound[i];
                nextRound.RealTimeMinersInformation[minerInRound.PublicKey] = new MinerInRound
                {
                    PublicKey = minersNotMinedCurrentRound[i].PublicKey,
                    Order = order,
                    ExpectedMiningTime = dateTime.ToTimestamp().GetArrangedTimestamp(order, miningInterval),
                    PromisedTinyBlocks = minerInRound.PromisedTinyBlocks,
                    ProducedBlocks = minerInRound.ProducedBlocks,
                    MissedTimeSlots = minerInRound.MissedTimeSlots + 1
                };
            }

            // Calculate extra block producer order and set the producer.
            var extraBlockProducerOrder = currentRound.CalculateNextExtraBlockProducerOrder();
            var expectedExtraBlockProducer =
                nextRound.RealTimeMinersInformation.Values.FirstOrDefault(m => m.Order == extraBlockProducerOrder);
            if (expectedExtraBlockProducer == null)
            {
                nextRound.RealTimeMinersInformation.Values.First().IsExtraBlockProducer = true;
            }
            else
            {
                expectedExtraBlockProducer.IsExtraBlockProducer = true;
            }

            return true;
        }

        private static int CalculateNextExtraBlockProducerOrder(this Round round)
        {
            var firstPlaceInfo = round.GetFirstPlaceMinerInformation();
            if (firstPlaceInfo == null)
            {
                // If no miner produce block during this round, just appoint the first miner to be the extra block producer of next round.
                return 1;
            }

            var signature = firstPlaceInfo.Signature;
            var sigNum = BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(), 0);
            var blockProducerCount = round.RealTimeMinersInformation.Count;
            var order = GetAbsModulus(sigNum, blockProducerCount) + 1;
            return order;
        }

        /// <summary>
        /// Get the first valid (mined) miner's information, which means this miner's signature shouldn't be empty.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public static MinerInRound GetFirstPlaceMinerInformation(this Round round)
        {
            return round.RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                .FirstOrDefault(m => m.Signature != null);
        }

        public static Hash CalculateSignature(this Round previousRound, Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in previousRound.RealTimeMinersInformation)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }

            return Hash.FromTwoHashes(inValue,
                previousRound.RealTimeMinersInformation.Values.Aggregate(Hash.Empty,
                    (current, minerInRound) => Hash.FromTwoHashes(current, minerInRound.Signature)));
        }

        public static Int64Value ToInt64Value(this long value)
        {
            return new Int64Value {Value = value};
        }

        public static StringValue ToStringValue(this string value)
        {
            return new StringValue {Value = value};
        }

        /// <summary>
        /// Include both min and max value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool InRange(this int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        public static Round GenerateFirstRoundOfNewTerm(this Miners miners, int miningInterval,
            DateTime currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
        {
            var dict = new Dictionary<string, int>();

            foreach (var miner in miners.PublicKeys)
            {
                dict.Add(miner, miner[0]);
            }

            var sortedMiners =
                (from obj in dict
                    orderby obj.Value descending
                    select obj.Key).ToList();

            var round = new Round();

            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                // The first miner will be the extra block producer of first round of each term.
                if (i == 0)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.PublicKey = sortedMiners[i];
                minerInRound.Order = i + 1;
                minerInRound.ExpectedMiningTime =
                    currentBlockTime.AddMilliseconds((i * miningInterval) + miningInterval).ToTimestamp();
                minerInRound.PromisedTinyBlocks = 1;
                // Should be careful during validation.
                minerInRound.PreviousInValue = Hash.Empty;

                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }

            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;

            return round;
        }

        public static Hash GetMinersHash(this Miners miners)
        {
            var orderedMiners = miners.PublicKeys.OrderBy(p => p);
            return Hash.FromString(orderedMiners.Aggregate("", (current, publicKey) => current + publicKey));
        }

        public static long GetMinedBlocks(this Round round)
        {
            return round.RealTimeMinersInformation.Values.Sum(minerInRound => minerInRound.ProducedBlocks);
        }

        public static void AddCandidate(this Candidates candidates, byte[] publicKey)
        {
            candidates.PublicKeys.Add(publicKey.ToHex());
            candidates.Addresses.Add(Address.FromPublicKey(publicKey));
        }

        public static bool RemoveCandidate(this Candidates candidates, byte[] publicKey)
        {
            var result1 = candidates.PublicKeys.Remove(publicKey.ToHex());
            var result2 = candidates.Addresses.Remove(Address.FromPublicKey(publicKey));
            return result1 && result2;
        }

        public static bool IsExpired(this VotingRecord votingRecord, long currentAge)
        {
            var lockExpiredAge = votingRecord.VoteAge;
            foreach (var day in votingRecord.LockDaysList)
            {
                lockExpiredAge += day;
            }

            return lockExpiredAge <= currentAge;
        }

        public static Miners ToMiners(this List<string> minerPublicKeys, long termNumber = 0)
        {
            return new Miners
            {
                PublicKeys = {minerPublicKeys},
                Addresses = {minerPublicKeys.Select(p => Address.FromPublicKey(ByteArrayHelpers.FromHexString(p)))},
                TermNumber = termNumber
            };
        }

        // TODO: Add test cases.
        /// <summary>
        /// Check the equality of time slots of miners.
        /// Also, the mining interval shouldn't be 0.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public static ValidationResult CheckTimeSlots(this Round round)
        {
            var miners = round.RealTimeMinersInformation.Values.OrderBy(m => m.Order).ToList();
            if (miners.Count == 1)
            {
                // No need to check single node.
                return new ValidationResult {Success = true};
            }

            if (miners.Any(m => m.ExpectedMiningTime == null))
            {
                return new ValidationResult {Success = false, Message = "Incorrect expected mining time."};
            }

            var baseMiningInterval =
                (miners[1].ExpectedMiningTime.ToDateTime() - miners[0].ExpectedMiningTime.ToDateTime())
                .TotalMilliseconds;

            if (baseMiningInterval <= 0)
            {
                return new ValidationResult {Success = false, Message = $"Mining interval must greater than 0.\n{round}"};
            }

            for (var i = 1; i < miners.Count - 1; i++)
            {
                var miningInterval =
                    (miners[i + 1].ExpectedMiningTime.ToDateTime() - miners[i].ExpectedMiningTime.ToDateTime())
                    .TotalMilliseconds;
                if (Math.Abs(miningInterval - baseMiningInterval) > baseMiningInterval)
                {
                    return new ValidationResult {Success = false, Message = "Time slots are so different."};
                }
            }

            return new ValidationResult {Success = true};
        }

        private static int GetAbsModulus(long longValue, int intValue)
        {
            return Math.Abs((int) longValue % intValue);
        }
    }
}