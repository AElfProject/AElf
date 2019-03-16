using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Consensus.DPoS
{
    public static class BasicExtensions
    {
        public static ConsensusCommand GetConsensusCommand(this DPoSBehaviour behaviour, Round round,
            MinerInRound minerInRound, int miningInterval, Timestamp timestamp, bool isBootMiner)
        {
            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    return new ConsensusCommand
                    {
                        // For now, only if one node configured himself as a boot miner can he actually create the first block,
                        // which block height is 2.
                        CountingMilliseconds = isBootMiner
                            ? 8000
                            : int.MaxValue,
                        // No need to limit the mining time for the first block a chain.
                        TimeoutMilliseconds = int.MaxValue,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.UpdateValue:
                    var expectedMiningTime = round.GetExpectedMiningTime(minerInRound.PublicKey);

                    return new ConsensusCommand
                    {
                        CountingMilliseconds = (int) (expectedMiningTime.ToDateTime() - timestamp.ToDateTime())
                            .TotalMilliseconds,
                        TimeoutMilliseconds = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.NextRound:
                    return new ConsensusCommand
                    {
                        CountingMilliseconds =
                            (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, timestamp).ToDateTime() -
                                   timestamp.ToDateTime()).TotalMilliseconds,
                        TimeoutMilliseconds = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.NextTerm:
                    return new ConsensusCommand
                    {
                        CountingMilliseconds =
                            (int) (round.ArrangeAbnormalMiningTime(minerInRound.PublicKey, timestamp).ToDateTime() -
                                   timestamp.ToDateTime()).TotalMilliseconds,
                        TimeoutMilliseconds = miningInterval / minerInRound.PromisedTinyBlocks,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                case DPoSBehaviour.Invalid:
                    return new ConsensusCommand
                    {
                        CountingMilliseconds = int.MaxValue,
                        TimeoutMilliseconds = 0,
                        Hint = new DPoSHint
                        {
                            Behaviour = behaviour
                        }.ToByteString()
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// This method is only executable when the miners of this round is more than 1.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public static int GetMiningInterval(this Round round)
        {
            if (round.RealTimeMinersInformation.Count < 2)
            {
                // Just appoint the mining interval for single miner.
                return 1000;
            }

            var firstTwoMiners = round.RealTimeMinersInformation.Values.Where(m => m.Order == 1 || m.Order == 2)
                .ToList();
            var distance =
                (int) (firstTwoMiners[1].ExpectedMiningTime.ToDateTime() -
                       firstTwoMiners[0].ExpectedMiningTime.ToDateTime())
                .TotalMilliseconds;
            return distance > 0 ? distance : -distance;
        }

        /// <summary>
        /// In current DPoS design, each miner produce his block in one time slot, then the extra block producer
        /// produce a block to terminate current round and confirm the mining order of next round.
        /// So totally, the time of one round is:
        /// MiningInterval * MinersCount + MiningInterval.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="miningInterval"></param>
        /// <returns></returns>                                                
        public static int TotalMilliseconds(this Round round, int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = round.GetMiningInterval();
            }

            return round.RealTimeMinersInformation.Count * miningInterval + miningInterval;
        }

        /// <summary>
        /// Actually the expected mining time of the miner whose order is 1.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public static Timestamp GetStartTime(this Round round)
        {
            return round.RealTimeMinersInformation.Values.First(m => m.Order == 1).ExpectedMiningTime;
        }

        /// <summary>
        /// This method for now is able to handle the situation of a miner keeping offline so many rounds,
        /// by using missedRoundsCount.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="miningInterval"></param>
        /// <param name="missedRoundsCount"></param>
        /// <returns></returns>
        public static Timestamp GetExpectedEndTime(this Round round, int missedRoundsCount = 0, int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = round.GetMiningInterval();
            }

            return round.GetStartTime().ToDateTime().AddMilliseconds(round.TotalMilliseconds(miningInterval))
                // Arrange an ending time if this node missed so many rounds.
                .AddMilliseconds(missedRoundsCount * round.TotalMilliseconds(miningInterval))
                .ToTimestamp();
        }

        /// <summary>
        /// Simply read the expected mining time of provided public key from round information.
        /// Do not check this node missed his time slot or not.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static Timestamp GetExpectedMiningTime(this Round round, string publicKey)
        {
            if (round.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return round.RealTimeMinersInformation[publicKey].ExpectedMiningTime;
            }

            return DateTime.MaxValue.ToUniversalTime().ToTimestamp();
        }

        /// <summary>
        /// For now, if current time is behind the end of expected mining time slot,
        /// we can say this node missed his time slot.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="publicKey"></param>
        /// <param name="timestamp"></param>
        /// <param name="minerInRound"></param>
        /// <returns></returns>
        public static bool IsTimeSlotPassed(this Round round, string publicKey, Timestamp timestamp,
            out MinerInRound minerInRound)
        {
            minerInRound = null;
            var miningInterval = round.GetMiningInterval();
            if (round.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                minerInRound = round.RealTimeMinersInformation[publicKey];
                return minerInRound.ExpectedMiningTime.ToDateTime().AddMilliseconds((double) miningInterval / 2) <
                       timestamp.ToDateTime();
            }

            return false;
        }

        /// <summary>
        /// If one node produced block this round or missed his time slot,
        /// whatever how long he missed, we can give him a consensus command with new time slot
        /// to produce a block (for terminating current round and start new round).
        /// The schedule generated by this command will be cancelled
        /// if this node executed blocks from other nodes.
        /// 
        /// Notice:
        /// This method shouldn't return the expected mining time from round information.
        /// To prevent this kind of misuse, this method will return a invalid timestamp
        /// when this node hasn't missed his time slot.
        /// </summary>
        /// <returns></returns>
        public static Timestamp ArrangeAbnormalMiningTime(this Round round, string publicKey, Timestamp timestamp,
            int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = round.GetMiningInterval();
            }

            if (!round.IsTimeSlotPassed(publicKey, timestamp, out var minerInRound) && minerInRound.OutValue == null)
            {
                return DateTime.MaxValue.ToUniversalTime().ToTimestamp();
            }

            if (round.GetExtraBlockProducerInformation().PublicKey == publicKey)
            {
                var distance = (round.GetExtraBlockMiningTime() - timestamp.ToDateTime()).TotalMilliseconds;
                if (distance > 0)
                {
                    return timestamp.ToDateTime().AddMilliseconds(distance).ToTimestamp();
                }
            }

            if (round.RealTimeMinersInformation.ContainsKey(publicKey) && miningInterval > 0)
            {
                var distanceToRoundStartTime =
                    (timestamp.ToDateTime() - round.GetStartTime().ToDateTime()).TotalMilliseconds;
                var missedRoundsCount = (int) (distanceToRoundStartTime / round.TotalMilliseconds(miningInterval));
                var expectedEndTime = round.GetExpectedEndTime(missedRoundsCount, miningInterval);
                return expectedEndTime.ToDateTime().AddMilliseconds(minerInRound.Order * miningInterval).ToTimestamp();
            }

            // Never do the mining if this node has no privilege to mime or the mining interval is invalid.
            return DateTime.MaxValue.ToUniversalTime().ToTimestamp();
        }

        public static MinerInRound GetExtraBlockProducerInformation(this Round round)
        {
            return round.RealTimeMinersInformation.First(bp => bp.Value.IsExtraBlockProducer).Value;
        }

        public static DateTime GetExtraBlockMiningTime(this Round round, int miningInterval = 0)
        {
            if (miningInterval == 0)
            {
                miningInterval = round.GetMiningInterval();
            }

            return round.RealTimeMinersInformation.OrderBy(m => m.Value.ExpectedMiningTime.ToDateTime()).Last().Value
                .ExpectedMiningTime.ToDateTime()
                .AddMilliseconds(miningInterval);
        }

        public static Round ApplyNormalConsensusData(this Round round, string publicKey, Hash previousInValue,
            Hash outValue, Hash signature, Timestamp timestamp)
        {
            if (round.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                round.RealTimeMinersInformation[publicKey].ActualMiningTime = timestamp;
                round.RealTimeMinersInformation[publicKey].PreviousInValue = previousInValue;
                round.RealTimeMinersInformation[publicKey].OutValue = outValue;
                if (round.RoundNumber != 1)
                {
                    round.RealTimeMinersInformation[publicKey].Signature = signature;
                }
                else
                {
                    signature = round.RealTimeMinersInformation[publicKey].Signature;
                }

                var minersCount = round.RealTimeMinersInformation.Count;
                var sigNum =
                    BitConverter.ToInt64(
                        BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(),
                        0);
                var orderOfNextRound = Math.Abs(GetModulus(sigNum, minersCount));

                // Check the existence of conflicts about OrderOfNextRound.
                // If so, modify others'.
                var conflicts = round.RealTimeMinersInformation.Values
                    .Where(i => i.OrderOfNextRound == orderOfNextRound).ToList();

                foreach (var minerInRound in conflicts)
                {
                    // Though multiple conflicts should be wrong, we can still arrange their orders of next round.

                    for (var i = minerInRound.Order + 1; i < minersCount * 2 + 1; i++)
                    {
                        if (round.RealTimeMinersInformation.Values.All(m => m.OrderOfNextRound != i))
                        {
                            round.RealTimeMinersInformation[minerInRound.PublicKey].OrderOfNextRound = i % minersCount;
                        }
                    }
                }

                round.RealTimeMinersInformation[publicKey].OrderOfNextRound = orderOfNextRound;
            }

            return round;
        }

        public static bool GenerateNextRoundInformation(this Round round, Timestamp timestamp,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            nextRound = new Round();

            // Check: If one miner's OrderOfNextRound isn't 0, his must published his signature.
            var minersMinedCurrentRound =
                round.RealTimeMinersInformation.Values.Where(m => m.OrderOfNextRound != 0).ToList();
            if (minersMinedCurrentRound.Any(m => m.Signature == null))
            {
                return false;
            }

            // TODO: Check: No order conflicts for next round.

            var miningInterval = round.GetMiningInterval();
            nextRound.RoundNumber = round.RoundNumber + 1;
            nextRound.TermNumber = round.TermNumber;
            nextRound.BlockchainAge =
                (long) (timestamp.ToDateTime() - blockchainStartTimestamp.ToDateTime()).TotalMinutes;

            // Set next round miners' information of miners successfully mined during this round.
            foreach (var minerInRound in minersMinedCurrentRound.OrderBy(m => m.OrderOfNextRound))
            {
                var order = minerInRound.OrderOfNextRound;
                nextRound.RealTimeMinersInformation[minerInRound.PublicKey] = new MinerInRound
                {
                    PublicKey = minerInRound.PublicKey,
                    Order = order,
                    ExpectedMiningTime = GetTimestampWithOffset(timestamp, miningInterval * order + miningInterval),
                    PromisedTinyBlocks = 1
                };
            }

            // Set miners' information of miners missed their time slot in this round.
            var minersNotMinedCurrentRound =
                round.RealTimeMinersInformation.Values.Where(m => m.OrderOfNextRound == 0).ToList();
            var minersCount = round.RealTimeMinersInformation.Count;
            var missedOrders = Enumerable.Range(1, minersCount).Where(i =>
                !round.RealTimeMinersInformation.Values.Select(m => m.OrderOfNextRound).ToList().Contains(i)).ToList();
            for (var i = 0; i < minersNotMinedCurrentRound.Count; i++)
            {
                var order = missedOrders[i];
                nextRound.RealTimeMinersInformation[minersNotMinedCurrentRound[i].PublicKey] = new MinerInRound
                {
                    PublicKey = minersNotMinedCurrentRound[i].PublicKey,
                    Order = order,
                    ExpectedMiningTime = GetTimestampWithOffset(timestamp, miningInterval * order),
                    PromisedTinyBlocks = 1
                };
            }

            var extraBlockProducerOrder = round.CalculateNextExtraBlockProducerOrder();
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
            var order = GetModulus(sigNum, blockProducerCount);
            return order;
        }

        private static Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
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

        public static Hash CalculateSignature(this Round round, Hash inValue)
        {
            // Check the signatures
            foreach (var minerInRound in round.RealTimeMinersInformation)
            {
                if (minerInRound.Value.Signature == null)
                {
                    minerInRound.Value.Signature = Hash.FromString(minerInRound.Key);
                }
            }

            return Hash.FromTwoHashes(inValue,
                round.RealTimeMinersInformation.Values.Aggregate(Hash.Empty,
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
            long currentRoundNumber = 0, long currentTermNumber = 0)
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

            // The extra block producer of first round is totally randomized.
            var selected = new Random().Next(0, miners.PublicKeys.Count);

            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                if (i == selected)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.PublicKey = sortedMiners[i];
                minerInRound.Order = i + 1;
                // Signatures totally randomized.
                minerInRound.Signature = Hash.Generate();
                minerInRound.ExpectedMiningTime =
                    GetTimestampOfUtcNow((i * miningInterval) + miningInterval);
                minerInRound.PromisedTinyBlocks = 1;

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

        public static bool IsTimeToChangeTerm(this Round round, Round previousRound, Timestamp blockchainStartTimestamp,
            long termNumber)
        {
            var minersCount = previousRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null);
            var minimumCount = ((int) ((minersCount * 2d) / 3)) + 1;
            var approvalsCount = round.RealTimeMinersInformation.Values.Where(m => m.ActualMiningTime != null)
                .Select(m => m.ActualMiningTime)
                .Count(t => IsTimeToChangeTerm(blockchainStartTimestamp, t, termNumber));
            return approvalsCount >= minimumCount;
        }

        public static long GetMinedBlocks(this Round round)
        {
            var minedBlocks = 0L;
            foreach (var minerInRound in round.RealTimeMinersInformation)
            {
                minedBlocks += minerInRound.Value.ProducedBlocks;
            }

            return minedBlocks;
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

        /// <summary>
        /// If DaysEachTerm == 7:
        /// 1, 1, 1 => 0 != 1 - 1 => false
        /// 1, 2, 1 => 0 != 1 - 1 => false
        /// 1, 8, 1 => 1 != 1 - 1 => true => term number will be 2
        /// 1, 9, 2 => 1 != 2 - 1 => false
        /// 1, 15, 2 => 2 != 2 - 1 => true => term number will be 3.
        /// </summary>
        /// <param name="blockchainStartTimestamp"></param>
        /// <param name="termNumber"></param>
        /// <param name="blockProducedTimestamp"></param>
        /// <returns></returns>
        private static bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
            long termNumber)
        {
            return (long) (blockProducedTimestamp.ToDateTime() - blockchainStartTimestamp.ToDateTime()).TotalMinutes /
                   ConsensusDPoSConsts.DaysEachTerm != termNumber - 1;
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

        private static int GetModulus(long longValue, int intValue)
        {
            return Math.Abs((int) (longValue %  intValue));
        }

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        private static Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            var now = Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
            return now;
        }
    }
}